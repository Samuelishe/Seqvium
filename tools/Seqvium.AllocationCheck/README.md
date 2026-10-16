# Realtime allocation counter diagnostic

This optional, hardware-independent console probe investigates positive current-thread byte deltas.
It references Core directly, without xUnit, MTP, Avalonia, devices or third-party packages. It is
outside the solution and is not an application launch mode or a replacement for strict audio tests.
The [R1 report](../../docs/experiments/SEQ-RT-ALLOCATION-AUDIT-R1_REPORT.md) explains the evidence and limits.

```powershell
dotnet restore tools/Seqvium.AllocationCheck --locked-mode
dotnet build tools/Seqvium.AllocationCheck -c Release --no-restore
dotnet tools/Seqvium.AllocationCheck/bin/Release/net10.0/Seqvium.AllocationCheck.dll empty thread none 64
dotnet tools/Seqvium.AllocationCheck/bin/Release/net10.0/Seqvium.AllocationCheck.dll empty thread background 64
dotnet tools/Seqvium.AllocationCheck/bin/Release/net10.0/Seqvium.AllocationCheck.dll preview thread background 64
dotnet tools/Seqvium.AllocationCheck/bin/Release/net10.0/Seqvium.AllocationCheck.dll mixer inline background 64
dotnet tools/Seqvium.AllocationCheck/bin/Release/net10.0/Seqvium.AllocationCheck.dll positive thread none 64
```

Arguments are scenario, consumer host, concurrent load and fixed observation count (1–128).

- `empty`: two counter reads surrounding one million integer arithmetic iterations and a primitive
  volatile write. No DSP, delegate, assertion, object construction or wait occurs inside the window.
- `spin`: a separate control surrounding `Thread.SpinWait(100000)` instead of arithmetic.
- `positive`: an escaping 37-byte array (64 managed bytes on the measured x64 runtime), plus the
  arithmetic interval. This demonstrates that real managed allocations remain detectable.
- `preview`: the historical 200 alternating 127/256-frame callbacks after one 256-frame warm-up.
- `mixer`: the historical EQ/compressor configuration, 20 warm-up and 50 measured 127-frame calls.
- `inline` uses the calling console thread; `thread` uses one joined dedicated consumer.
- `none` has no load worker. `allocate` replaces a bounded array ring without explicit collections.
  `background` additionally requests nonblocking Gen2 collections from that separate worker.

Load is diagnostic intervention, never a normal test setting. There are at most 1024 producer
iterations, 8 MiB ring payload plus one replacement and approximately 4 MiB linked small objects.
The producer sleeps 2 ms between iterations, stops when observations finish and joins within five
seconds. Observations have a 45-second cancellation deadline and the consumer a 60-second join bound.
Preparation/media, resets, one varying-size allocation-context anchor, markers, timestamps, GC counts,
thread IDs, position/voice validation, result storage and JSON serialization are outside both counter
reads. No GC disabling, NoGCRegion, retry, discarded sample, subtraction or tolerance is applied.

Each audio observation restarts the prepared execution outside measurement. Preview must advance
256 → 38556 frames and retain one playing voice; Mixer must advance 2540 → 8890 and retain one voice.
The probe warms its own entry once before concurrent load, unlike the original tests. It does not
repeat the Mixer's partition-parity rendering; that assertion remains in the unchanged original test.
The original zero-valued Preview PCM is retained, so active position/voice checks prevent a silent
stopped-branch false pass. Generated managed media uses a fresh temporary directory and is removed
after joined execution. No user project or physical device is used.

Stdout is one JSON object with every signed delta, managed/native consumer thread, surrounding GC
counts, QPC timestamps and audio position. Exit 0 means every observation equals the strict expected
value (zero, or 64 for `positive`); exit 1 preserves an anomaly, exit 2 rejects arguments. Setup,
timeout and cleanup exceptions are failures. Collection counts bracket a wider interval than the byte
counter and record collection starts, not completion; equal counts do not exclude an overlapping BGC.

For a bounded matrix, run each of `empty`, `positive`, `preview`, `mixer` once with both hosts and all
three loads at 64 observations: 24 processes / 1536 windows. Preserve every process exit code and JSON.
Do not repeat until a preferred result appears. The report records the actual matrix, including red.

For optional EventPipe capture, install a local tool into an ignored directory:

```powershell
dotnet tool install dotnet-trace --version 10.0.750501 --tool-path .artifacts/rt-allocation-tools
.artifacts/rt-allocation-tools/dotnet-trace collect --profile gc-verbose --providers Seqvium-AllocationCheck --output .artifacts/empty-background.nettrace --show-child-io -- dotnet tools/Seqvium.AllocationCheck/bin/Release/net10.0/Seqvium.AllocationCheck.dll empty thread background 64
```

`Seqvium-AllocationCheck/MeasurementBegin` (ID 1, observation index) and `MeasurementEnd` (ID 2,
index/delta) bracket the counter window on its native thread. Both are outside the counter reads;
their trace interval is a superset. Events are disabled in the unprofiled control. Inspect these
markers with runtime GC events and allocation stacks in an EventPipe-capable viewer. Validate marker
counts and event loss. `gc-verbose` samples allocations; absence of a sampled event cannot exclude
small allocations, including the known 64-byte positive control. Keep profiled results separate.
