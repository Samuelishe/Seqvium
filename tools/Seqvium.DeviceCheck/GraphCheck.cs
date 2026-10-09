// SPDX-License-Identifier: Apache-2.0
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using Seqvium.Audio.Windows;
using Seqvium.Core;

[SupportedOSPlatform("windows")]
internal static class GraphCheck
{
    private sealed record Load(string Name, bool Cpu, bool Allocations, bool Edits, bool Forced,
        bool Legacy = false, bool Trace = false, string? Endpoint = null);
    private readonly record struct CollectionInterval(long Begin, long End, int Cycle);

    internal static async Task<int> Diagnose(string[] args)
    {
        if (args.Length is < 4 or > 6) throw new ArgumentException(
            "graph-diagnose full|natural|forced|legacy|cpu|allocations|edits <1..120 seconds> <1..3 repeats> [endpoint ID] [gc-trace]");
        int seconds = int.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture);
        int repeats = int.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture);
        if (seconds is < 1 or > 120 || repeats is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(seconds));
        bool trace = args[^1] == "gc-trace";
        string? endpoint = args.Length > 4 && args[4] != "gc-trace" ? args[4] : null;
        var load = args[1] switch
        {
            "full" => new Load("full", true, true, true, true),
            "natural" => new Load("natural", true, true, true, false),
            "forced" => new Load("forced", false, false, false, true),
            "legacy" => new Load("legacy", true, true, true, true, Legacy: true),
            "cpu" => new Load("cpu", true, false, false, false),
            "allocations" => new Load("allocations", false, true, false, false),
            "edits" => new Load("edits", false, false, true, false),
            _ => throw new ArgumentException("Unknown diagnostic variant.")
        };
        load = load with { Trace = trace, Endpoint = endpoint };
        Console.WriteLine(JsonSerializer.Serialize(new { Mode = "graph-diagnose", seconds, repeats, load,
            Environment = RuntimeInformation.OSDescription, Runtime = RuntimeInformation.FrameworkDescription,
            Stopwatch.Frequency, GCSettings.IsServerGC, GcLatency = GCSettings.LatencyMode.ToString(),
            Environment.ProcessorCount, ProcessId = Environment.ProcessId }));
        bool accepted = true;
        for (int repeat = 1; repeat <= repeats; repeat++)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { Repeat = repeat, Begin = Stopwatch.GetTimestamp() }));
            accepted &= await Configuration(seconds, true, load);
        }
        return accepted ? 0 : 1;
    }
    private sealed class Owner : SynchronizationContext
    {
        private readonly ConcurrentQueue<Action> _queue = new();
        public override void Post(SendOrPostCallback callback, object? state) => _queue.Enqueue(() => callback(state));
        internal void Invoke(Action action)
        {
            var previous = Current;
            SetSynchronizationContext(this);
            try { action(); } finally { SetSynchronizationContext(previous); }
        }
        internal void Pump() { while (_queue.TryDequeue(out var action)) Invoke(action); }
        internal async Task Complete(Func<Task> operation)
        {
            Task task = Task.CompletedTask;
            Invoke(() => task = operation());
            while (!task.IsCompleted) { Pump(); await Task.Delay(1); }
            await task; Pump();
        }
    }

    private sealed class Fixture : IDisposable
    {
        internal readonly string Root = Path.Combine(Path.GetTempPath(), "Seqvium-R4-F2-" + Guid.NewGuid().ToString("N"));
        internal readonly ProjectDocument Document = ProjectDocument.Create();
        internal readonly float[] Pcm = Enumerable.Range(0, 44100).Select(index =>
            (float)(0.012 * Math.Sin(index * 2 * Math.PI * 220 / 44100) + 0.003 * (1 - index / 44100.0))).ToArray();
        internal readonly Id<MusicalPart>[] Parts = new Id<MusicalPart>[8];
        internal readonly GraphNode[] Gains = new GraphNode[8];
        internal GraphAttachment Attachment = null!;
        internal GraphNode Mix = null!;
        internal Id<Pattern> Pattern;
        internal Id<SoundDefinition> Sound;
        internal Id<ResourceDescriptor> Resource;
        internal async Task Initialize(string layout)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, true))
            {
                writer.Write("RIFF"u8); writer.Write(36 + Pcm.Length * 4); writer.Write("WAVEfmt "u8);
                writer.Write(16); writer.Write((ushort)3); writer.Write((ushort)1); writer.Write(44100);
                writer.Write(176400); writer.Write((ushort)4); writer.Write((ushort)32);
                writer.Write("data"u8); writer.Write(Pcm.Length * 4);
                foreach (float value in Pcm) writer.Write(value);
            }
            stream.Position = 0;
            using var import = ProjectMedia.BeginImport(Document, "Authored quiet graph PCM",
                releaseMilliseconds: 20, ownedMediaDirectory: Root);
            await import.PrepareAsync(stream);
            var media = import.Accept(); Sound = media.SoundId; Resource = media.ResourceId;
            Document.Edit("Maximum supported local graph", edit =>
            {
                Pattern = edit.AddPattern("Bounded graph repeat", new(960000));
                var placement = edit.AddPlacement(Pattern, new(0));
                Attachment = edit.CreateItemGraph(placement, true);
                Mix = edit.AddGraphNode(Attachment.GraphId, GraphBuiltIns.Mix, new(0, 0), layout, 8);
                for (int index = 0; index < 8; index++)
                {
                    Parts[index] = edit.AddPart(Pattern, "Independent part " + index, Sound);
                    edit.AddNote(Pattern, Parts[index], new(index * 4000), new(720000), 60 + index * 0.05m, 0.2m);
                    var source = edit.AddGraphNode(Attachment.GraphId, GraphBuiltIns.Source, new(0, 0), layout);
                    Gains[index] = edit.AddGraphNode(Attachment.GraphId, GraphBuiltIns.Gain, new(0, 0), layout);
                    edit.SetGraphGain(Attachment.GraphId, Gains[index].Id, 0.25m + index * 0.05m);
                    edit.BindGraphSource(Attachment.Id, source.Id, placement, Parts[index]);
                    Connect(edit, source, Gains[index]); Connect(edit, Gains[index], Mix, index);
                }
                var previous = Mix;
                for (int index = 0; index < 14; index++)
                {
                    var gain = edit.AddGraphNode(Attachment.GraphId, GraphBuiltIns.Gain, new(0, 0), layout);
                    if (index == 13) edit.SetGraphGain(Attachment.GraphId, gain.Id, 0.2m);
                    Connect(edit, previous, gain); previous = gain;
                }
                var output = edit.AddGraphNode(Attachment.GraphId, GraphBuiltIns.Output, new(0, 0), layout);
                Connect(edit, previous, output); edit.SetGraphOutput(Attachment.Id, output.Id);
            });
        }
        private void Connect(ProjectEdit edit, GraphNode from, GraphNode to, int slot = 0) => edit.ConnectGraphPorts(
            Attachment.GraphId, from.Id, from.Ports.Single(port => port.Direction == GraphPortDirection.Output).Id,
            to.Id, to.Ports.Where(port => port.Direction == GraphPortDirection.Input).ElementAt(slot).Id);

        // Independent source/schedule/envelope arithmetic. No prepared events, execution or graph operations.
        internal double OracleError(ReadOnlySpan<float> captured, int rate, int channels)
        {
            var graph = Document.Current.State.Graphs.Single();
            int[] order = Mix.Ports.Where(port => port.Direction == GraphPortDirection.Input).OrderBy(port => port.Id.Value)
                .Select(port => Array.FindIndex(Gains, gain => gain.Id == graph.Connections.Single(edge => edge.ToPortId == port.Id).FromNodeId)).ToArray();
            long Frame(long ticks) => (long)decimal.Round(ticks * rate / 1920000m, 0, MidpointRounding.AwayFromZero);
            int release = rate / 50;
            double error = 0;
            for (int frame = 0; frame < captured.Length / channels; frame++)
            {
                float sum = 0;
                int loop = frame * 2 / rate;
                foreach (int part in order)
                {
                    float contribution = 0;
                    for (int iteration = Math.Max(0, loop - 1); iteration <= loop + 1; iteration++)
                    {
                        long on = Frame(iteration * 960000L + part * 4000);
                        long off = Frame(iteration * 960000L + part * 4000 + 720000);
                        long age = frame - on;
                        if (age < 0 || frame >= off + release) continue;
                        double cursor = age * 44100.0 / rate * Math.Pow(2, part * 0.05 / 12);
                        int index = (int)cursor;
                        if (index >= Pcm.Length) continue;
                        double value = Pcm[index] + ((index + 1 < Pcm.Length ? Pcm[index + 1] : 0) - Pcm[index]) * (cursor - index);
                        double envelope = frame < off ? 1 : 1 - (frame - off) / (double)release;
                        contribution += (float)(value * 0.2f * envelope);
                    }
                    sum += contribution * (float)(0.25m + part * 0.05m);
                }
                float expected = sum * 0.2f;
                for (int channel = 0; channel < channels; channel++)
                    error = Math.Max(error, Math.Abs(captured[frame * channels + channel] - expected));
            }
            return error;
        }
        public void Dispose()
        {
            if (!Document.IsClosed) Document.Close();
            // Only the exclusively created UUID fixture, after joined execution, is removed.
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }
    }

    internal static async Task<int> Run(string[] args)
    {
        int seconds = args.Length > 1 ? int.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 60;
        if (seconds is < 1 or > 120) throw new ArgumentOutOfRangeException(nameof(seconds));
        Console.WriteLine(JsonSerializer.Serialize(new { Mode = "graph-measure", seconds,
            Environment = RuntimeInformation.OSDescription, Runtime = RuntimeInformation.FrameworkDescription,
            Stopwatch.Frequency, GCSettings.IsServerGC, GcLatency = GCSettings.LatencyMode.ToString(),
            Topology = "8 Source, 22 Gain, 8-input Mix, Output; 32 nodes, 31 connections" }));
        bool accepted = await Configuration(seconds, false);
        accepted &= await Configuration(seconds, true);
        for (int index = 0; index < 3; index++) accepted &= await Fault(index == 2, index);
        Console.WriteLine(JsonSerializer.Serialize(new { GraphAccepted = accepted, BoundedLongRun = seconds >= 60,
            AcousticAssessment = "Owner review remains separate; render-buffer capture is not DAC/capture-loopback evidence." }));
        return accepted ? 0 : 1;
    }

    private static async Task<bool> Configuration(int seconds, bool pressure, Load? diagnostic = null)
    {
        var load = diagnostic ?? new Load(pressure ? "graph-pressure" : "graph-baseline", pressure, pressure, pressure, pressure);
        using var output = new WasapiOutput(176400, load.Endpoint, diagnostics: true);
        if (output.Facts is not { } facts)
        {
            output.Close(); Console.WriteLine(JsonSerializer.Serialize(new { Run = "graph-open", output.Failure, output.HResult }));
            return false;
        }
        using var fixture = new Fixture();
        await fixture.Initialize(facts.Channels == 1 ? GraphBuiltIns.Mono : GraphBuiltIns.Stereo);
        ProjectDocument? legacy = null;
        if (load.Legacy)
        {
            string path = Path.Combine(fixture.Root, "legacy.seqvium");
            ProjectPersistence.Save(fixture.Document, path);
            legacy = ProjectPersistence.Open(path).Document;
            legacy.Edit("Graph-free diagnostic projection", edit =>
            {
                foreach (var placement in legacy.Current.State.Placements) edit.DeletePlacement(placement.Id);
            });
        }
        using var plan = legacy is null ? GraphPreparation.Prepare(fixture.Document, fixture.Attachment.Id, facts.Rate,
            facts.Channels, facts.CapacityFrames, repeats: 1024) :
            SamplerPreparation.PreparePattern(legacy, fixture.Pattern, facts.Rate, facts.Channels, repeats: 1024);
        float[] warmPacket = new float[256 * facts.Channels];
        using (var warm = plan.CreateExecution()) for (int iteration = 0; iteration < 3000; iteration++) warm.Process(warmPacket);
        var sampler = new RealtimeSampler(facts.Rate, facts.Channels, facts.CapacityFrames);
        var owner = new Owner();
        GraphExecutionCoordinator coordinator = null!;
        using var loadCancellation = new CancellationTokenSource();
        Task cpu = Task.CompletedTask;
        try
        {
            if (legacy is null)
                owner.Invoke(() => coordinator = new(fixture.Document, sampler, fixture.Attachment.Id, owner, repeats: 1024));
            else
            {
                using var initial = sampler.BeginPreparation(legacy, fixture.Pattern, repeats: 1024);
                await initial.PrepareAsync();
                if (initial.Publish() != SamplerPublication.Accepted) throw new InvalidOperationException("Legacy preparation refused.");
            }
            var waiting = Stopwatch.StartNew();
            while (!sampler.HasPending && waiting.Elapsed.TotalSeconds < 5) { owner.Pump(); await Task.Delay(1); }
            bool started = false;
            if (legacy is null) owner.Invoke(() => started = coordinator.Start());
            else { sampler.Start(); started = true; sampler.SetGain(0.1f); }
            if (!started) throw new InvalidOperationException("Graph did not prepare current target.");
            var ring = new byte[128][];
            cpu = load.Cpu ? Task.Run(() =>
            {
                double value = 0.1;
                while (!loadCancellation.IsCancellationRequested)
                    for (int index = 0; index < 10000; index++) value = Math.Sin(value + 0.001);
                GC.KeepAlive(value);
            }) : Task.CompletedTask;
            int cycles = 0, edits = 0, ringIndex = 0, sourceEdits = 0, allocations = 0, forcedCount = 0;
            var forced = new CollectionInterval[128];
            using var trace = load.Trace ? new GcTrace() : null;
            int[] beforeGc = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
            long beforeAllocated = GC.GetTotalAllocatedBytes(true);
            TimeSpan beforePause = GC.GetTotalPauseDuration();
            long measurementBegin = Stopwatch.GetTimestamp();
            output.Start(sampler);
            var elapsed = Stopwatch.StartNew();
            while (elapsed.Elapsed.TotalSeconds < seconds && output.Failure == OutputFailure.None)
            {
                owner.Pump(); await Task.Delay(10); cycles++;
                if (load.Allocations)
                    for (int index = 0; index < 16; index++) { ring[ringIndex++ % ring.Length] = new byte[32768]; allocations++; }
                if (load.Edits && cycles % 10 == 0) owner.Invoke(() =>
                {
                    fixture.Document.Edit("Live branch coefficient", edit => edit.SetGraphGain(fixture.Attachment.GraphId,
                        fixture.Gains[0].Id, cycles % 20 == 0 ? 0.25m : 0.4m)); edits++;
                });
                if (load.Edits && cycles % 50 == 0) owner.Invoke(() =>
                {
                    fixture.Document.Edit("Source revision replacement", edit => edit.ConfigurePcmSampler(fixture.Sound,
                        fixture.Resource, cycles % 100 == 0 ? 60 : 60.05m, 20)); edits++; sourceEdits++;
                });
                if (load.Legacy && load.Edits && cycles % 50 == 0 && output.Failure == OutputFailure.None)
                {
                    legacy!.Edit("Equivalent legacy source revision", edit => edit.ConfigurePcmSampler(fixture.Sound,
                        fixture.Resource, cycles % 100 == 0 ? 60 : 60.05m, 20));
                    sampler.InvalidatePreparation(); sampler.RetireCompleted();
                    using var replacement = sampler.BeginPreparation(legacy, fixture.Pattern, repeats: 1024);
                    await replacement.PrepareAsync();
                    if (replacement.Publish() != SamplerPublication.Accepted && output.Failure == OutputFailure.None)
                        throw new InvalidOperationException("Legacy replacement refused.");
                }
                if (load.Forced && cycles % 100 == 0)
                {
                    long begin = Stopwatch.GetTimestamp();
                    GC.Collect(2, GCCollectionMode.Forced, true, true);
                    forced[forcedCount++] = new(begin, Stopwatch.GetTimestamp(), cycles);
                }
            }
            long measurementEnd = Stopwatch.GetTimestamp();
            long processAllocated = GC.GetTotalAllocatedBytes(true) - beforeAllocated;
            int[] collections = beforeGc.Select((count, index) => GC.CollectionCount(index) - count).ToArray();
            double pauseMilliseconds = (GC.GetTotalPauseDuration() - beforePause).TotalMilliseconds;
            loadCancellation.Cancel(); await cpu;
            long stop = sampler.Stop();
            bool stopAck = await Acknowledge(sampler, stop, owner);
            int stopVoices = sampler.ActiveVoices;
            long panic = sampler.Panic();
            bool panicAck = await Acknowledge(sampler, panic, owner);
            var status = coordinator?.ReadStatus();
            long retainedBytes = sampler.RetainedPcmAndScratchBytes;
            if (coordinator is not null) await owner.Complete(coordinator.CloseAsync);
            output.Close(); sampler.RetireCompleted(); sampler.Dispose();
            if (coordinator is not null) await owner.Complete(async () => await coordinator.DisposeAsync());
            // The workload counters end before Stop/Panic, joined cleanup, JSON and offline/oracle work.
            var observations = output.Observations.ToArray().Where(item => item.ServiceStart <= measurementEnd).ToArray();
            if (trace is not null) await Task.Delay(250); // Drain asynchronous runtime delivery after consumer join.
            var gcEvents = trace?.Snapshot(measurementBegin, measurementEnd);
            double? oracle = pressure ? null : fixture.OracleError(output.Capture, facts.Rate, facts.Channels);
            double? parity = null;
            if (!pressure)
            {
                using var execution = plan.CreateExecution();
                int offset = 0; double maximum = 0;
                foreach (int frames in output.CapturePartitions)
                {
                    float[] block = new float[frames * facts.Channels]; execution.Process(block);
                    for (int index = 0; index < block.Length; index++) maximum = Math.Max(maximum, Math.Abs(block[index] - output.Capture[offset + index]));
                    offset += block.Length;
                }
                parity = maximum;
            }
            bool accepted = output.Failure == OutputFailure.None && output.ElapsedSeconds >= seconds && output.Joined &&
                output.ProcessorMisses == 0 && output.ServiceMisses == 0 && output.PacketMisses == 0 && output.PaddingExhaustions == 0 &&
                output.ProcessorAllocatedBytes == 0 && output.NonFiniteSamples == 0 && output.MaximumObservedVoices == 8 &&
                stopAck && panicAck && stopVoices == 0 && sampler.LivePreparedStates == 0 &&
                (pressure || oracle <= 0.000002 && parity <= 0.000002);
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Run = load.Name, Accepted = accepted, facts, output.DiagnosticsEnabled, load,
                output.ElapsedSeconds, output.CallbackCount, output.Failure, output.HResult, output.UnexpectedError,
                ProcessorMicroseconds = Quantiles(observations.Select(item => item.ProcessorTicks)),
                ServiceMicroseconds = Quantiles(observations.Select(item => item.ServiceTicks)),
                WakeMicroseconds = Quantiles(observations.Select(item => item.WakeTicks)),
                GcPauseMilliseconds = pauseMilliseconds,
                PacketMinimum = observations.Length == 0 ? (int?)null : observations.Min(item => item.Frames),
                PacketMaximum = observations.Length == 0 ? (int?)null : observations.Max(item => item.Frames),
                output.ProcessorMisses, output.ServiceMisses, output.PacketMisses, output.PaddingExhaustions,
                output.ProcessorAllocatedBytes, output.ServiceAllocatedBytes, output.WorkerAllocatedBytes, processAllocated,
                Collections = collections,
                output.MaximumObservedVoices, output.NonFiniteSamples, output.SilentPacketsAfterStop,
                output.MaximumSampleMagnitude, output.MaximumAdjacentSampleDelta, output.MaximumPacketBoundaryDelta,
                CaptureSamples = output.Capture.Length, OracleError = oracle, OfflineParityError = parity,
                plan.DecodedBytes, plan.ScratchBytes, plan.PacketWork, plan.MaximumPacketEvents,
                GraphPreparation.MaximumPacketWork, retainedBytes, RealtimeSampler.MaximumRetainedPcmAndScratchBytes,
                RealtimeSampler.MaximumPreparedTableAllowanceBytes, output.DiagnosticStorageBytes,
                cycles, edits, sourceEdits, allocations, AllocatedPayloadBytes = (long)allocations * 32768,
                ForcedCollections = forced.Take(forcedCount).ToArray(), CpuWorkers = load.Cpu ? 1 : 0,
                measurementBegin, measurementEnd, output.PlaybackStart, output.PlaybackEnd, output.WorkerThreadId,
                MeasurementSeconds = (measurementEnd - measurementBegin) / (double)Stopwatch.Frequency,
                CounterScope = "Process/GC/load: workload boundary; output/lifetime: playback through Stop/Panic and join",
                Trace = gcEvents,
                ProblemWindow = ProblemWindow(observations),
                status, stopAck, panicAck, stopVoices, output.Joined,
                sampler.PreparedStatesCreated, sampler.PreparedStatesReleased, sampler.Retirements, sampler.LivePreparedStates
            }));
            GC.KeepAlive(ring);
            return accepted;
        }
        finally
        {
            loadCancellation.Cancel(); await cpu;
            if (coordinator is not null) await owner.Complete(coordinator.CloseAsync);
            output.Close();
            if (output.Joined)
            {
                sampler.Dispose();
                if (coordinator is not null) await owner.Complete(async () => await coordinator.DisposeAsync());
            }
            legacy?.Close();
        }
    }

    private static ServiceObservation[] ProblemWindow(ServiceObservation[] observations)
    {
        if (observations.Length == 0) return [];
        int worst = 0;
        for (int index = 1; index < observations.Length; index++)
            if (observations[index].Padding == 0 || observations[index].WakeTicks > observations[worst].WakeTicks) worst = index;
        return observations.Skip(Math.Max(0, worst - 5)).Take(11).ToArray();
    }

    private static async Task<bool> Acknowledge(RealtimeSampler sampler, long command, Owner owner)
    {
        var elapsed = Stopwatch.StartNew();
        while (sampler.StopAcknowledgment < command && elapsed.Elapsed.TotalSeconds < 2 && !sampler.IsTerminated)
        { owner.Pump(); await Task.Delay(5); }
        return sampler.StopAcknowledgment >= command;
    }

    private static double[]? Quantiles(IEnumerable<long> ticks)
    {
        var sorted = ticks.Order().ToArray();
        return sorted.Length == 0 ? null : new[] { 0.5, 0.95, 0.99, 1 }
            .Select(q => sorted[(int)Math.Ceiling(sorted.Length * q) - 1] * 1e6 / Stopwatch.Frequency).ToArray();
    }

    private static async Task<bool> Fault(bool stall, int index)
    {
        using var output = new WasapiOutput(0, diagnostics: true);
        if (output.Facts is not { } facts) { output.Close(); return false; }
        using var fixture = new Fixture();
        await fixture.Initialize(facts.Channels == 1 ? GraphBuiltIns.Mono : GraphBuiltIns.Stereo);
        var sampler = new RealtimeSampler(facts.Rate, facts.Channels, facts.CapacityFrames);
        using var request = sampler.BeginGraphPreparation(fixture.Document, fixture.Attachment.Id, repeats: 1024);
        await request.PrepareAsync();
        if (request.Publish() != SamplerPublication.Accepted) throw new InvalidOperationException("Fault plan refused.");
        sampler.Start(); output.Start(sampler);
        await Task.Delay(100);
        long beforeStopAck = sampler.StopAcknowledgment;
        if (stall) output.InjectWorkerStall(); else output.InjectDeviceInvalidation();
        var elapsed = Stopwatch.StartNew();
        while (!sampler.IsTerminated && elapsed.Elapsed.TotalSeconds < 3) await Task.Delay(5);
        output.Close(); sampler.RetireCompleted(); sampler.Dispose();
        bool accepted = sampler.IsTerminated && output.Joined && output.Failure != OutputFailure.None &&
            sampler.StopAcknowledgment == beforeStopAck && sampler.LivePreparedStates == 0;
        Console.WriteLine(JsonSerializer.Serialize(new { Run = "graph-fault-lifetime", index, stall, Accepted = accepted,
            output.Failure, output.HResult, output.Joined, sampler.StopAcknowledgment, beforeStopAck,
            sampler.TerminationBoundaries, sampler.ActiveVoices, sampler.LivePreparedStates }));
        return accepted;
    }
}
