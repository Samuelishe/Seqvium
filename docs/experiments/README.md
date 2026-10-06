# Experiments and evidence

Role: Entry point and reporting convention for bounded technical experiments.
Read when: Starting a probe or locating evidence supporting a technical choice.
Authoritative for: Experiment discovery, report structure, evidence-versus-production boundary.
Not authoritative for: Accepted architecture, implementation status, or future stage order.

No experiment has been run and no executable probe exists yet. The first intended experiment is
[SEQ-R0 Audio Architecture Probe](../ROADMAP.md#seq-r0--audio-architecture-probe), guided by
[AUDIO_ENGINE](../AUDIO_ENGINE.md) and [KNOWN_PROBLEMS](../KNOWN_PROBLEMS.md).

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
[DECISIONS_LOG](../DECISIONS_LOG.md). Narrow/close questions in [KNOWN_PROBLEMS](../KNOWN_PROBLEMS.md)
with evidence links; completed experiment facts belong in [WORK_LOG](../WORK_LOG.md).
