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
