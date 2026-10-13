// SPDX-License-Identifier: Apache-2.0
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;
using Seqvium.Audio.Windows;
using Seqvium.Core;

[SupportedOSPlatform("windows")]
internal static class LoopCheck
{
    private sealed class Owner : SynchronizationContext
    {
        private readonly ConcurrentQueue<Action> _queue = new();
        public override void Post(SendOrPostCallback callback, object? state) => _queue.Enqueue(() => callback(state));
        internal void Run(Action action)
        {
            var previous = Current; SetSynchronizationContext(this);
            try { action(); } finally { SetSynchronizationContext(previous); }
        }
        internal void Pump() { while (_queue.TryDequeue(out var action)) Run(action); }
        internal async Task Complete(Func<Task> action)
        {
            Task work = Task.CompletedTask; Run(() => work = action());
            while (!work.IsCompleted) { Pump(); await Task.Delay(5); }
            await work; Pump();
        }
    }

    internal static async Task<int> Run()
    {
        Console.Error.WriteLine("Loop smoke plays a quiet authored sine for six seconds; system volume is unchanged.");
        string root = Path.Combine(Path.GetTempPath(), "Seqvium-loop-" + Guid.NewGuid().ToString("N"));
        var document = ProjectDocument.Create();
        var owner = new Owner();
        GraphExecutionCoordinator? coordinator = null;
        RealtimeSampler? sampler = null;
        using var output = new WasapiOutput(192000, diagnostics: true);
        if (output.Failure != OutputFailure.None || output.Facts is not { } facts)
        {
            Console.Error.WriteLine(JsonSerializer.Serialize(new { output.Failure, output.HResult })); document.Close(); return 1;
        }
        try
        {
            using var bytes = new MemoryStream();
            using (var writer = new BinaryWriter(bytes, System.Text.Encoding.ASCII, true))
            {
                writer.Write("RIFF"u8); writer.Write(36 + 24000 * 4); writer.Write("WAVEfmt "u8);
                writer.Write(16); writer.Write((ushort)3); writer.Write((ushort)1); writer.Write(48000); writer.Write(192000);
                writer.Write((ushort)4); writer.Write((ushort)32); writer.Write("data"u8); writer.Write(24000 * 4);
                for (int i = 0; i < 24000; i++) writer.Write((float)(.05 * Math.Sin(i * 2 * Math.PI * 220 / 48000)));
            }
            bytes.Position = 0;
            using var import = ProjectMedia.BeginImport(document, "Quiet sine", releaseMilliseconds: 0, ownedMediaDirectory: root);
            await import.PrepareAsync(bytes);
            Id<Pattern> pattern = default; Id<MusicalPart> part = default; GraphAttachment attachment = null!;
            import.Accept((edit, media) =>
            {
                pattern = edit.AddPattern("Loop", new(960000)); part = edit.AddPart(pattern, "Sine", media.SoundId);
                edit.AddNote(pattern, part, default, new(960000), 60, 1);
                var placement = edit.AddPlacement(pattern, default); attachment = edit.CreateItemGraph(placement);
                string layout = facts.Channels == 1 ? GraphBuiltIns.Mono : GraphBuiltIns.Stereo;
                var source = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Source, new(0, 0), layout);
                var sink = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Output, new(200, 0), layout);
                edit.BindGraphSource(attachment.Id, source.Id, placement, part);
                edit.ConnectGraphPorts(attachment.GraphId, source.Id, source.Ports.Single().Id, sink.Id, sink.Ports.Single().Id);
                edit.SetGraphOutput(attachment.Id, sink.Id);
            });
            sampler = new(facts.Rate, facts.Channels, facts.CapacityFrames); sampler.SetGain(.1f); sampler.SetLooping(true);
            owner.Run(() => coordinator = new(document, sampler, attachment.Id, owner));
            var timeout = Stopwatch.StartNew(); bool started = false;
            while (!started && timeout.Elapsed < TimeSpan.FromSeconds(10))
            { owner.Pump(); owner.Run(() => started = coordinator!.Start()); if (!started) await Task.Delay(5); }
            if (!started) throw new InvalidOperationException("Loop preparation did not become ready.");
            output.Start(sampler);
            long epoch = 0, beforeEdit = 0; bool edited = false, converged = false;
            timeout.Restart();
            while (timeout.Elapsed < TimeSpan.FromSeconds(6) && output.Failure == OutputFailure.None)
            {
                owner.Pump(); var status = sampler.ReadStatus();
                if (epoch == 0 && status.Playing) epoch = status.Epoch;
                if (!edited && timeout.Elapsed >= TimeSpan.FromSeconds(2.5))
                {
                    beforeEdit = status.Position;
                    owner.Run(() => document.Edit("Live extra note", edit => edit.AddNote(pattern, part, new(480000), new(480000), 60, .25m)));
                    edited = true;
                }
                if (edited && status.ExecutingRevision == document.Current.Revision) converged = true;
                await Task.Delay(5);
            }
            var final = sampler.ReadStatus(); long stop = 0; owner.Run(() => stop = coordinator!.Stop());
            timeout.Restart();
            while (sampler.StopAcknowledgment < stop && timeout.Elapsed < TimeSpan.FromSeconds(1))
            { owner.Pump(); await Task.Delay(5); }
            await owner.Complete(() => coordinator!.CloseAsync());
            output.Close(); sampler.TerminateExecution();
            await owner.Complete(async () => await coordinator!.DisposeAsync());
            sampler.RetireCompleted();
            bool passed = output.Failure == OutputFailure.None && final.Playing && final.Looping && converged &&
                final.Epoch == epoch && final.Position > beforeEdit && final.Position > facts.Rate * 5L &&
                sampler.StopAcknowledgment >= stop && output.Joined && output.ProcessorAllocatedBytes == 0 &&
                output.NonFiniteSamples == 0 && output.ProcessorMisses == 0 && output.PacketMisses == 0;
            Console.WriteLine(JsonSerializer.Serialize(new { Mode = "loop-smoke", Passed = passed, facts, final,
                LiveEditConverged = converged, SameEpoch = final.Epoch == epoch, output.ElapsedSeconds, output.Failure,
                output.ProcessorAllocatedBytes, output.ServiceAllocatedBytes, output.MaximumSampleMagnitude,
                output.MaximumPacketBoundaryDelta, output.ProcessorMisses, output.PacketMisses, output.PaddingExhaustions,
                StopAcknowledged = sampler.StopAcknowledgment >= stop, output.Joined,
                AcousticAssessment = "Render/output evidence only; owner listening remains separate." }));
            return passed ? 0 : 1;
        }
        finally
        {
            if (coordinator is not null) await owner.Complete(() => coordinator.CloseAsync());
            output.Close();
            sampler?.TerminateExecution();
            if (coordinator is not null) await owner.Complete(async () => await coordinator.DisposeAsync());
            sampler?.Dispose();
            document.Close();
            // Exclusively created UUID fixture; cleanup follows joined output and preparation.
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
