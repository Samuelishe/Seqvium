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
5. Make the smallest coherent change and validate in proportion to its risk.
6. Update durable documents in the same change only when their owned truth changes.

Do not depend on earlier chat history or load the whole knowledge base by default.
Do not delete or overwrite user work to make the repository cleaner. Report material pre-existing changes.
Do not commit, push, pull, fetch, merge, rebase, reset, clean, restore, or checkout user files without
explicit user authorization. A suggested commit message is not authorization. Do not alter the staging
index without an explicit request. Read-only Git inspection is appropriate.

## Context routing

All routes start with current state; add these owners as needed:

| Task | Read |
| --- | --- |
| Product / feature / UX | [PROJECT_VISION](docs/PROJECT_VISION.md), [UX_CONTRACT](docs/UX_CONTRACT.md) |
| Visual system / interaction design quality | [UI_DESIGN](docs/UI_DESIGN.md), UX contract; workspace or graph owner for affected behavior |
| Architecture | [ARCHITECTURE](docs/ARCHITECTURE.md) |
| Audio / realtime | [AUDIO_ENGINE](docs/AUDIO_ENGINE.md), architecture |
| Node / processing graph, ports, node execution | [NODE_GRAPH](docs/NODE_GRAPH.md) + affected audio/architecture owner |
| Workspace panes, docking, floating, layout, activation | [WORKSPACE](docs/WORKSPACE.md), UX contract |
| Samples / generation / resampling | [SAMPLE_WORKFLOW](docs/SAMPLE_WORKFLOW.md); audio or extensions for affected boundaries |
| Extensions / packages | [EXTENSIONS](docs/EXTENSIONS.md) |
| Project serialization | [PROJECT_FORMAT](docs/PROJECT_FORMAT.md); extensions for opaque state |
| C# implementation / refactoring | [CODING_GUIDELINES](docs/CODING_GUIDELINES.md), architecture + affected owner |
| Development environment / local tooling / SDK | [DEVELOPMENT](docs/DEVELOPMENT.md) |
| Tests / verification | [TEST_EXECUTION](docs/TEST_EXECUTION.md) + affected owner and actual test setup |
| Portability / platform-specific work | [PORTABILITY](docs/PORTABILITY.md) + affected architecture owner |
| CI / hosted automation / release validation | [CI_CD](docs/CI_CD.md); test execution / portability as needed |
| Planning | [ROADMAP](docs/ROADMAP.md) |
| Speculative ideas / alternatives | [IDEAS](docs/IDEAS.md) |
| Existing implementation compromises | [TECH_DEBT](docs/TECH_DEBT.md) |
| Concrete unresolved risks / evidence gaps | [KNOWN_PROBLEMS](docs/KNOWN_PROBLEMS.md), [DECISIONS_LOG](docs/DECISIONS_LOG.md) |
| Documentation / ownership | [DOCUMENTATION_GOVERNANCE](docs/DOCUMENTATION_GOVERNANCE.md) |
| Third-party dependencies / resources | [THIRD_PARTY](docs/THIRD_PARTY.md) + affected owner |
| Experiments | [Experiment guide](docs/experiments/README.md) + affected technical owner |

## Change and verification

Keep proposals distinct from accepted constraints and implemented behavior. Add an abstraction only
for concrete ownership or testability needs. Foundation documentation is not permission to implement
future roadmap stages. Experiments provide evidence; they are not production architecture.

Documentation changes require relative-link, ownership, decision-status, scope, and diff checks.
Code changes later require focused builds/tests and relevant runtime evidence; inspect the actual
test platform before choosing commands. Do not create tests or CI merely for appearance.
Update owners when contracts change, current state when its facts change, and the work log only for
meaningful completed work. Follow [governance](docs/DOCUMENTATION_GOVERNANCE.md) for conflicts.
Finish with verification results and final Git status, distinguishing task changes from existing work.
When a task creates or modifies tracked files, always include one concise suggested English commit
message describing the actual outcome. Begin with the stage/milestone ID when applicable. If no tracked
files changed, no message is needed. A suggested message never authorizes commit or push.
