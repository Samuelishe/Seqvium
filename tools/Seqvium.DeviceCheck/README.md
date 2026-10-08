# Explicit Windows audio verification

This small Release harness exercises production Core PCM execution through `Seqvium.Audio.Windows`.
It plays quiet project-authored WAV material on the current default console output; it changes no
device/volume/rate settings. It is not the future workstation, an engine SDK or an ordinary unit test.

From the repository root:

```text
dotnet restore Seqvium.sln --locked-mode
dotnet build Seqvium.sln -c Release --no-restore
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll smoke 2
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll measure 60
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll lifetimes
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll faults
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll pressure 60
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll audition-smoke
```

`measure 60` runs baseline and GC/control pressure for each of 1/4/8 voices, ten fresh lifetimes,
then stall/invalidation/unavailable-endpoint checks. Exit 0 means the requested checks passed; exit 1
records a failed acceptance check. A shorter measure run cannot establish the full protocol.
`pressure 60` separately exercises eight voices and explicitly counts cancellation/supersession issued
while the preparation task is incomplete. It does not replace the full baseline/pressure/lifetime series.
Redirect JSON-lines output into ignored `bin/` for local evidence. Percentiles are median/p95/p99/worst
in microseconds. Failure enums are emitted as numeric values in order declared by `OutputFailure`.
The [protocol](../../docs/experiments/SEQ-R2-F2_PROTOCOL.md) fixes acceptance/instrumentation;
the [report](../../docs/experiments/SEQ-R2-F2_REPORT.md) records observations and limits.

Source material is authored mathematically in `Program.cs`, written as WAV through an independent
writer, imported into an exclusively owned temporary lifecycle directory and decoded by production
F1. Only that fixture directory is removed after joined output and released sampler/preparation owners.
No personal files, music samples or third-party audio are read. Binary/log/capture artifacts are local
ignored outputs; no audio asset, external codec, native sampler DLL or global tool is introduced.

Control/document mutation is caller-serialized. `BeginPreparation` captures the immutable revision and
media roots on that owner. Await preparation before Publish/Dispose. Only one candidate may own
decoded preparation resources, including a completed unpublished result. Invalidate preparation when
canonical authority/target changes; pending consumer generations then reject stale handoff. The
identified old active revision can continue. Reclaim retired state on the control owner.
Close the output and successfully join before disposing its sampler; a join timeout retains ownership.
Transport Stop/Start restarts within the stream; fault recovery uses a fresh output session and new epoch.

## Transient preview smoke and diagnostic mode

All commands warn on stderr that sound **will play**. `audition-smoke` authors an external one-second
220 Hz sine WAV (amplitude 0.02) in a private UUID fixture, with initial preview gain 0.05. It imports
nothing and checks two fresh sessions: normal diagnostics-off, then opt-in bounded capture/observations.
Checks include initial silence, source deletion after preparation, EOF, Stop, replacement, live cancel,
project Close, unchanged canonical state/resources/history and joined temporary-PCM release.
An independent raw-source interpolation oracle verifies the first complete captured preview and silence.
This is not acoustic latency or a full F2 performance series; details are in the
[F3 report](../../docs/experiments/SEQ-R2-F3_REPORT.md).

`WasapiOutput()` defaults to no capture/observation/partition payload or per-service allocation/finite/
silence instrumentation. Clock/padding/fault-stop and resource ownership remain enabled. Default-mode
zero timing/allocation fields mean **not measured**. All existing F2 harness paths explicitly pass
`diagnostics: true`, preserving reporting and instrumentation capacity failure. Supplying a nonzero
capture capacity without diagnostics is refused; arrays stay bounded when enabled. No system volume,
sample-rate, endpoint preference or other audio setting is changed.
