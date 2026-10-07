# Cold archive index

Role: Historical lookup map.
Read when: Explicit history, provenance, rationale, supersession, or reconstruction is needed.
Authoritative for: Historical records and traceability only.
Not authoritative for: Current behavior, policy, plans, implementation state, or open questions.

This is a cold, non-canonical archive excluded from normal current context. Active owner documents
win any conflict; historical wording records its stage, not a competing current specification.

## Authority and retrieval boundary

`docs/archive/**` is historical, non-canonical for current product behavior, and non-authoritative
whenever it conflicts with active owner documents. It is read only for explicit history, provenance,
why, supersession, resolved-question reconstruction, or previous implementation/validation tasks.

Exclude it from normal startup, selective owner reading, ordinary task context, generated current-context
maps, and future default RAG/index corpora. `docs/**` current knowledge excluding `docs/archive/**`
and the archive are separate retrieval classes; historical retrieval explicitly opts in. Do not copy
history back into active docs to force default retrieval. No executable retrieval infrastructure exists.

## Historical lookup

| Record | Use |
| --- | --- |
| [DECISIONS](DECISIONS.md) | Acceptance, rationale, refinements/supersessions and stable D-IDs |
| [RESOLVED_QUESTIONS](RESOLVED_QUESTIONS.md) | Resolved questions/portions, current owners and remaining related questions |
| [ROADMAP](ROADMAP.md) | Completed knowledge stages and superseded planning structure |
| [WORK_LOG](WORK_LOG.md) | Meaningful completed work and historical validation facts |
| [AUDITS](AUDITS.md) | Past audits, ranking and checkpoint context |

Keep this initial archive flat. Split by year/stage only when actual size/use justifies it. Useful
resolved idea/debt/removed-component history may use AUDITS until a separate split is justified;
current licensing obligations always remain in [THIRD_PARTY](../THIRD_PARTY.md). Current rules and
future update triggers belong to [governance](../DOCUMENTATION_GOVERNANCE.md#rolling-current-knowledge-and-cold-history).
