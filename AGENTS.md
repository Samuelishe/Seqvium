# Seqvium agent entry point

Role: Operational entry point for repository agents.
Read when: Starting every repository task.
Authoritative for: Startup, repository safety, context routing, verification, and agent workflow.
Not authoritative for: Product contracts, architecture detail, current state, roadmap, or history.

## Startup and safety

For every nontrivial task:

1. Read this file and [PROJECT_STATE](docs/PROJECT_STATE.md).
2. Inspect Git status and existing files; preserve staged, unstaged, and untracked user work.
3. Select only the needed owners below or through [INDEX](docs/INDEX.md).
4. Inspect affected source/tests if they exist; use `rg --files` and `rg` for discovery.
5. Make a coherent change within the user's authorized scope and validate in proportion to its risk.
   An explicitly requested whole-interface redesign can replace existing compositions, tokens and
   guidelines together; do not reduce that request to cosmetic changes to preserve an older baseline.
6. Update durable documents in the same change only when their owned truth changes.

Do not depend on earlier chat history or load the whole knowledge base by default.
Active docs contain current truth/policy/plans/open questions/state; history belongs in `docs/archive/`.
Exclude `docs/archive/**` from normal startup, selected owner reading, ordinary task context, generated
current-context maps and future default RAG/index corpora. Current docs (excluding archive) and history
are separate retrieval classes. Read archive only for explicit history/provenance/why/supersession,
resolved-question reconstruction or previous implementation/validation tasks; historical retrieval opts in.
Do not delete or overwrite user work to make the repository cleaner. Report material pre-existing changes.
Do not commit, push, pull, fetch, merge, rebase, reset, clean, restore, or checkout user files without
explicit user authorization. A suggested commit message is not authorization. Do not alter the staging
index without an explicit request. Read-only Git inspection is appropriate.

External-repository research is working context, not automatic public documentation. Follow the
[public research boundary](docs/DOCUMENTATION_GOVERNANCE.md#external-repository-research-and-public-documentation);
record actual distributed third-party provenance separately in THIRD_PARTY.

Before changing documentation, identify its audience and follow the
[audience and editorial policy](docs/DOCUMENTATION_GOVERNANCE.md#audiences-and-document-purpose).
Keep human product pages separate from agent handoff/routing, preserve canonical owners, and check
affected public descriptions whenever behavior or setup changes.

## Context routing

All routes start with current state; add these owners as needed:

| Task                                                      | Read                                                                                                              |
|-----------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------|
| Product / feature / UX                                    | [PROJECT_VISION](docs/PROJECT_VISION.md), [UX_CONTRACT](docs/UX_CONTRACT.md)                                      |
| Visual system / interaction design quality                | [UI_DESIGN](docs/UI_DESIGN.md), [DESIGN_SYSTEM](docs/DESIGN_SYSTEM.md), UX contract; workspace or graph owner for affected behavior |
| Architecture                                              | [ARCHITECTURE](docs/ARCHITECTURE.md)                                                                              |
| Audio / realtime                                          | [AUDIO_ENGINE](docs/AUDIO_ENGINE.md), architecture                                                                |
| Node / processing graph, ports, node execution            | [NODE_GRAPH](docs/NODE_GRAPH.md) + affected audio/architecture owner                                              |
| Workspace panes, docking, floating, layout, activation    | [WORKSPACE](docs/WORKSPACE.md), UX contract                                                                       |
| User/application preferences, reset, production logging   | [SETTINGS](docs/SETTINGS.md); project format for project-owned persistence, development for developer diagnostics |
| Samples / generation / resampling                         | [SAMPLE_WORKFLOW](docs/SAMPLE_WORKFLOW.md); audio or extensions for affected boundaries                           |
| Extensions / packages                                     | [EXTENSIONS](docs/EXTENSIONS.md)                                                                                  |
| Project serialization                                     | [PROJECT_FORMAT](docs/PROJECT_FORMAT.md); extensions for opaque state                                             |
| C# implementation / refactoring                           | [CODING_GUIDELINES](docs/CODING_GUIDELINES.md), architecture + affected owner                                     |
| Development environment / local tooling / SDK             | [DEVELOPMENT](docs/DEVELOPMENT.md)                                                                                |
| Product version / checkpoint classification / About identity | [VERSIONING](docs/VERSIONING.md); test execution for metadata verification                                      |
| Tests / verification                                      | [TEST_EXECUTION](docs/TEST_EXECUTION.md) + affected owner and actual test setup                                   |
| ProjectStats / structural repository diagnostics          | [PROJECT_STATS](docs/PROJECT_STATS.md), development and test execution for introduction/verification              |
| Portability / platform-specific work                      | [PORTABILITY](docs/PORTABILITY.md) + affected architecture owner                                                  |
| CI / hosted automation / release validation               | [CI_CD](docs/CI_CD.md); test execution / portability as needed                                                    |
| Planning                                                  | [ROADMAP](docs/ROADMAP.md)                                                                                        |
| Speculative ideas / alternatives                          | [IDEAS](docs/IDEAS.md)                                                                                            |
| Existing implementation compromises                       | [TECH_DEBT](docs/TECH_DEBT.md)                                                                                    |
| Concrete unresolved risks / evidence gaps                 | [KNOWN_PROBLEMS](docs/KNOWN_PROBLEMS.md); current contracts stay in their active owners                           |
| Historical decision / resolved question / completed work  | [Cold archive](docs/archive/INDEX.md), only when explicitly needed; never default current context                 |
| Documentation / ownership                                 | [DOCUMENTATION_GOVERNANCE](docs/DOCUMENTATION_GOVERNANCE.md)                                                      |
| Project licensing / third-party dependencies or resources | [THIRD_PARTY](docs/THIRD_PARTY.md), root [LICENSE](LICENSE) for license text + affected owner                     |
| Experiments                                               | [Experiment guide](docs/experiments/README.md) + affected technical owner                                         |

## Change and verification

Keep proposals distinct from accepted constraints and implemented behavior. Add an abstraction only
for concrete ownership or testability needs. Foundation documentation is not permission to implement
future roadmap stages. Experiments provide evidence; they are not production architecture.

For production/test/product resource changes, apply [versioning](docs/VERSIONING.md) checkpoint gates.
Codex may decide a justified verified PATCH or BUILD; MAJOR/MINOR always require the owner. Read the
current source/ledger, classify the completed checkpoint once, and record any bump and evidence there.

Documentation changes require relative-link, ownership, decision-status, scope, and diff checks.
Code changes later require focused builds/tests and relevant runtime evidence; inspect the actual
test platform before choosing commands. Do not create tests or CI merely for appearance.
Follow [TEST_EXECUTION](docs/TEST_EXECUTION.md#required-verification-levels): ordinary code tasks
require L0 + focused L1; add L2 for affected interactions/contracts. L3 is a full Release build/suite
about every 10 completed tasks, earlier for concrete substantial risk or release checkpoints, never
automatically for each handoff, BUILD or commit. Record cadence in PROJECT_STATE. Select L4 GUI/native/
stress/physical evidence independently by risk/request. Docs-only changes use static checks.
New tests must cover a specific behavior/contract, regression, boundary or failure; prefer observable
behavior, small owned fixtures and Theory for repeated inputs. Avoid duplicate coverage without a
distinct boundary, timing sleeps, order/user-state/device dependencies and implementation-only assertions.
Aim for focused verification within 120 seconds; measure overruns, use valid incremental/no-build/
no-restore outputs and MTP filters, and reuse sufficient green checks until relevant changes/failures.
Report the actual scope; focused verification is never a full regression pass.
Every first-party UI task must read DESIGN_SYSTEM and use its shared tokens/components, including
DrawingContext code. Shared-style changes require the component catalog and rendered field-alignment
checks; update the implementation when the visual contract changes.
First-party left sidebars must use the ordered LeftPanelZone/SidePanelHost composition under
[Workspace](docs/WORKSPACE.md#shared-left-panel-zone). Reuse its right-boundary ownership, minimum-content
clipping, near-edge collapse, independent visibility/width restoration and layout persistence; do not
add per-panel resize policies, overlay windows or misleading draggable captions. Exercise neighbouring
panels together, cancellation/reversal and reduced-window mouse input after changes to this zone.
Before designing or changing any first-party UI, inspect relevant actual screenshots in the local
reference library linked by [UI design](docs/UI_DESIGN.md#reference-library-and-next-editor-direction).
If suitable local screenshots are unavailable, search the web and inspect real product interfaces
before editing. A catalog entry or remembered interface is not a visual review. Never ship permanent
instructional paragraphs, developer explanations, formulas or redundant technical counters on a
musical working surface. Prefer recognizable controls/icons and one- or two-word action labels.
Keep essential musical values and concise actionable errors; put exact/technical detail behind a
deliberate disclosure and keep tooltips short. Review populated and empty states for text/numeric
overload at ordinary and reduced sizes, in both languages/themes. This is a required UI gate for all agents.
Update owners when contracts change and current state when its facts change. Move resolved questions
and completed roadmap stages out of active registers; append decision rationale and meaningful
completed work directly to the archive. Preserve current truth in active owners, not only in history.
Follow [governance](docs/DOCUMENTATION_GOVERNANCE.md) for conflicts.
Finish with verification results and final Git status, distinguishing task changes from existing work.
At the end of every substantial task, inspect untracked content too and distinguish product source,
permanent tests/runtime resources, user work and disposable task outputs. Preserve all required build/
runtime material and reusable verification tools; remove only task-owned temporary files no longer needed.
Keep transient GUI screenshots in ignored local artifact directories, repair documentation references
after cleanup and report intentionally retained artifacts with their reason. Follow the
[evidence retention policy](docs/experiments/README.md#evidence-retention) and
[resource placement policy](docs/CODING_GUIDELINES.md#resource-and-shared-code-placement).
Do not use broad Git cleanup/history/index mutations to achieve cleanliness.
When a task creates or modifies tracked files, always include one concise suggested English commit
message describing the actual outcome. Begin with the stage/milestone ID when applicable. If no tracked
files changed, no message is needed. A suggested message never authorizes commit or push.
At the end of repository-changing work, refresh the ignored root PROJECT_STATS.md and PROJECT_STATS.json
with the current ProjectStats executable, replacing the previous generated files. Read and apply
[ProjectStats](docs/PROJECT_STATS.md#completion-workflow): inspect corpus/metadata, unreadable counts and
advisories; link the Markdown report and summarize meaningful size/concentration findings in the final
agent response. Keep measured structural counts separate from executed tests and do not treat size
advisories as permission for unrelated refactoring. Rebuild the tool only when missing or changed.
