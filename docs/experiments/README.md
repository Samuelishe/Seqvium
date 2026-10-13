# Experiments and evidence

Role: Entry point and reporting convention for bounded technical experiments.
Read when: Starting a probe or locating evidence supporting a technical choice.
Authoritative for: Experiment discovery, evidence retention, report structure, evidence-versus-production boundary.
Not authoritative for: Accepted architecture, implementation status, or future stage order.

The [SEQ-R0 Audio Architecture Probe](../ROADMAP.md#seq-r0--audio-architecture-probe) has a
[pre-measurement protocol](SEQ-R0_PROTOCOL.md), [measured report](SEQ-R0_REPORT.md), and
[standalone source/build/run guide](../../experiments/seq-r0/README.md). Its observations do not
adopt an engine/backend/ABI or authorize R1. [AUDIO_ENGINE](../AUDIO_ENGINE.md) and
[KNOWN_PROBLEMS](../KNOWN_PROBLEMS.md) retain canonical contracts and open questions.

## Bounded probe discipline

SEQ-R2-F2's [protocol](SEQ-R2-F2_PROTOCOL.md), [report](SEQ-R2-F2_REPORT.md) and
[explicit Release harness](../../tools/Seqvium.DeviceCheck/README.md) evaluate production F1 PCM
through the scoped Windows output adapter. Reports own measurements; audio/architecture owners retain
the implemented contract. F2 neither promotes R0 experimental plumbing nor selects a permanent engine.

State the question and scope before coding. A minimal host should test a risky boundary without
becoming an early production application. Keep experiment code separate from future production code;
the exact code location/build shape should be selected when a concrete probe exists.

Do not infer architectural acceptance from a working demo. Record failures and limitations as well
as successful measurements. Production must not depend on the probe's host, UI, or types.

SEQ-R2-F3's [source/preview report and R2 audit](SEQ-R2-F3_REPORT.md) records deterministic source
access/reuse and explicitly audible transient preview smoke. It distinguishes normal diagnostics-off
from opt-in capture and post-change F2 regressions; it does not replace F2's full performance protocol.

SEQ-R2-F4's [endpoint report and complete R2 audit](SEQ-R2-F4_REPORT.md) records silent Windows
render/capture discovery and explicitly selected output/join/reinitialization checks. Input enumeration
and selection do not establish capture/recording capability; full F2 workload evidence remains distinct.

SEQ-R3-F1's [desktop report](SEQ-R3-F1_REPORT.md) records actual window/input and compact Dark/Light
visual observations, separate from pure host tests. Disposable generated F1 screenshots were removed
from the current tracked tree once their essential observations were recorded in Markdown; they were
not product resources or the owner's third-party design references.

SEQ-R3-F2's [workspace report](SEQ-R3-F2_REPORT.md) records bounded pane/state/persistence verification,
actual Windows overlap/dock/collapse/focus/restart observations. Earlier generated screenshots remain
only in ignored local artifact directories; the report does not depend on those files. The owner now
accepts the bounded F2 interaction level. SEQ-R3-CLOSE records the completed foundation audit in cold
history, preserves useful F1/F2 evidence and generates no screenshots or raw logs. Local acceptance
is distinct from wider platform and delivered-release evidence.

## Evidence retention

[Desktop foundation verification](SEQ-DESKTOP-FOUNDATIONS-F1_REPORT.md) records Design System v1,
rendered field alignment, Workspace gestures and Single Instance process evidence. Its reusable host
belongs in tools; renders/synthetic projects/raw JSON stay ignored locally. Native foreground refusal
is separate from actual-window render/routed evidence and does not establish visual owner acceptance.

Git is product source of truth, not an experiment-output warehouse. By default temporary screenshots,
raw captures, logs and one-off probe outputs remain local and ignored under `.artifacts/` or `.scratch/`.
Do not commit GUI screenshots/raw test output or create a permanent report for every minor iteration.
These are distinct from authored source, intentional runtime resources/test fixtures and user data.

- Put small reproducible cases in regular tests, and genuinely reusable verification tools in the
  appropriate `tests/` or `tools/` location
  under [development policy](../DEVELOPMENT.md#verification-output-organization).
- Significant measurements may retain compact Markdown reports containing methods, important failures,
  observations and limits. Completed minor experiments instead contribute meaningful conclusions to
  their authoritative subsystem owner and, where appropriate, existing archived decision/work history.
- Prefer approximately 1–3 current detailed working investigation reports when sufficient. Consolidate
  or archive superseded narratives by relevance, not a blind quota; technically important evidence
  must not be discarded to reach a number. Reports own observations, not accepted product contracts.
- Preserve exceptional high-value evidence when its loss would erase reproducibility or decision
  context: the original R0 and R2-F2 realtime measurements/protocols remain such evidence. Other reports
  may stay while they support distinct current acceptance/ownership decisions; do not delete all history.
- Exceptional retained raw evidence needs an explicit engineering reason, bounded scope and provenance.
  Generated images ignored under `docs/experiments/` are not the runtime-asset location. Do not hide
  production resources with global extension ignores; placement is owned by
  [coding guidelines](../CODING_GUIDELINES.md#resource-and-shared-code-placement).

Before removing outputs, preserve user work, required build/runtime material, tests and reusable harnesses.
Remove only disposable task-owned data or explicitly authorized tracked artifacts; update references
and report any local artifacts deliberately kept, their location/reason and review/deletion boundary.
Do not stage, rewrite history or use broad Git cleanup commands. A local-only screenshot location is
not a durable Markdown link target or a promise that every clone contains those images.

## Rolling retention v1

Developer verification applies the executable policy in
[ArtifactRetention](../../tools/Seqvium.VisualCheck/ArtifactRetention.cs). Its `Policy` defaults are
listed here; other tooling guides link this owner rather than repeat the settings.

| Constraint | Default / deterministic order |
| --- | --- |
| Completed success age | 7 days |
| Completed failure age | 14 days |
| Runs per producer/scenario | At most 3 eligible completed runs |
| Mandatory evidence reserve | Latest success and latest failure, ordered by completion time then ordinal path |
| Total artifact size | Soft 1 GiB budget |

Reservations consume count slots and override age/count/budget. Fill remaining slots with newest
runs, then select unreserved runs exceeding age or count. If the projected tree still exceeds the
budget, select remaining eligible runs oldest first, breaking equal-time ties by ordinal path.
Pinned exceptional evidence, unknown trees, live owners and incomplete runs are never budget victims;
an excess caused by protected data is reported. Active/incomplete runs are outside the completed-run
quota until completion and owner exit. Explicit preservation needs a concrete engineering reason,
scope and review boundary; do not pin every ordinary result.

`catalog`, `matrix`, `gestures`, the focused `steps` and small lifecycle `smoke` create a unique run beneath the supplied
artifact parent. The process script creates its own unique run. They automatically apply retention
before verification and after normal joined completion. New `.seqvium-run.json` manifests identify
owner/schema, producer, scenario, run ID, PID plus process start time, timestamps and success/failure.
A held `.active` lease also protects the GUI run. Watchdog diagnostics and exit 124 record timeout;
a run interrupted before its completion marker or a parent with an unjoined child remains incomplete
and protected. The watchdog covers automatic maintenance too, and does not wait for an additional
completion-marker write before exiting.

Deletion requires a valid completed manifest, absent owner, free lease, no active executable/command
line referring to the target, no nested ownership and a wholly readable tree without reparse points.
The process snapshot is bounded; its failure protects all candidates. Windows uses the existing
PowerShell/CIM environment, Linux uses `/proc`; other platforms conservatively retain candidates.
This is portability-aware tooling, not evidence of other-OS Desktop acceptance. Canonical root/ancestor
checks and an exclusive root cleanup lock guard deletion. If that lock is busy, optional automatic
rotation skips with diagnostics; explicit manual apply reports that cleanup did not run and returns
nonzero. No scan-based deletion or retention-journal write proceeds without lock ownership. Each file is checked for links and exclusive access;
partial failures keep the manifest for diagnostics/retry and do not stop independent candidates.
The local deletion journal rotates into one previous log, rather than growing without a bound.
Dry-run neither writes ownership/log files nor deletes anything.

Legacy data has no immunity but is never inferred disposable from age alone. The finalize inventory
reviewed SDK dependency/runtime manifests, generated `obj` sources/file lists and absent process owners
before adopting exact old build/cache directories as `disposable` with a `ReviewReason`. Completed
F1 JSON/TRX families were separately adopted for rotation. Everything else stays unmarked/protected
until a similarly specific review; notably unresolved R4-F2 raw traces and owner project/media/review
material were retained. Original F1 render/native raw evidence has explicit preservation reasons.
Build-copy deletion removes generated outputs, not the temporary harness source kept for provenance.
Manual planning/application commands are in [VisualCheck](../../tools/Seqvium.VisualCheck/README.md#artifact-retention).

## Report contents

A report under this directory should include:

- Question/hypothesis and candidate alternatives.
- Scope, acceptance criteria, and explicitly untested behavior.
- Environment: OS, hardware/device where relevant, SDK/compiler/library versions, sample rate,
  buffer settings, and build configuration.
- Reproducible setup and commands, input material/provenance, and measurement method.
- Observed results, stress/failure behavior, evidence paths, and known measurement limitations.
- Recommendation: accept, reject, narrow, or investigate further, with rationale.

Keep only selected durable findings here under the retention policy above; disposable captures/logs
remain local. Offline render evidence and
real device/callback evidence must remain distinguishable.

When a result justifies a durable decision, update the affected technical owner and
append rationale/history to [archived decisions](../archive/DECISIONS.md). Narrow current questions
in [KNOWN_PROBLEMS](../KNOWN_PROBLEMS.md); remove resolved questions and preserve their answers/evidence
in [resolved history](../archive/RESOLVED_QUESTIONS.md). Completed experiment facts go directly to
[archived work](../archive/WORK_LOG.md). These write destinations do not make archive routine context;
reports retain current useful evidence, while obsolete reports/validation narratives move to cold history.
