# Experiments and evidence

Role: Entry point and reporting convention for bounded technical experiments.
Read when: Starting a probe or locating evidence supporting a technical choice.
Authoritative for: Experiment discovery, report structure, evidence-versus-production boundary.
Not authoritative for: Accepted architecture, implementation status, or future stage order.

The [SEQ-R0 Audio Architecture Probe](../ROADMAP.md#seq-r0--audio-architecture-probe) has a
[pre-measurement protocol](SEQ-R0_PROTOCOL.md), [measured report](SEQ-R0_REPORT.md), and
[standalone source/build/run guide](../../experiments/seq-r0/README.md). Its observations do not
adopt an engine/backend/ABI or authorize R1. [AUDIO_ENGINE](../AUDIO_ENGINE.md) and
[KNOWN_PROBLEMS](../KNOWN_PROBLEMS.md) retain canonical contracts and open questions.

## Bounded probe discipline

State the question and scope before coding. A minimal host should test a risky boundary without
becoming an early production application. Keep experiment code separate from future production code;
the exact code location/build shape should be selected when a concrete probe exists.

Do not infer architectural acceptance from a working demo. Record failures and limitations as well
as successful measurements. Production must not depend on the probe's host, UI, or types.

## Report contents

A report under this directory should include:

- Question/hypothesis and candidate alternatives.
- Scope, acceptance criteria, and explicitly untested behavior.
- Environment: OS, hardware/device where relevant, SDK/compiler/library versions, sample rate,
  buffer settings, and build configuration.
- Reproducible setup and commands, input material/provenance, and measurement method.
- Observed results, stress/failure behavior, evidence paths, and known measurement limitations.
- Recommendation: accept, reject, narrow, or investigate further, with rationale.

Keep durable findings here; bulky generated captures/logs may later use a documented local artifact
location. Do not add an artifact framework before a probe requires it. Offline render evidence and
real device/callback evidence must remain distinguishable.

When a result justifies a durable decision, update the affected technical owner and
append rationale/history to [archived decisions](../archive/DECISIONS.md). Narrow current questions
in [KNOWN_PROBLEMS](../KNOWN_PROBLEMS.md); remove resolved questions and preserve their answers/evidence
in [resolved history](../archive/RESOLVED_QUESTIONS.md). Completed experiment facts go directly to
[archived work](../archive/WORK_LOG.md). These write destinations do not make archive routine context;
reports retain current useful evidence, while obsolete reports/validation narratives move to cold history.
