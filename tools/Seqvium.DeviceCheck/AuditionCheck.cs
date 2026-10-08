// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using Seqvium.Core;
using Seqvium.Audio.Windows;

[SupportedOSPlatform("windows")]
internal static class AuditionCheck
{
    internal static async Task<int> Run()
    {
        Console.Error.WriteLine(
            "Audition smoke: quiet authored WAV, initial gain 0.05; raw/solo only. Sound WILL be played.");
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Mode = "audition-smoke", Environment = RuntimeInformation.OSDescription,
            Runtime = RuntimeInformation.FrameworkDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(), WavAudition.InitialGain
        }));
        string root = Path.Combine(Path.GetTempPath(), "Seqvium-F3-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        bool accepted = true;
        try
        {
            foreach (bool diagnostics in new[] { false, true }) accepted &= await Session(root, diagnostics);
        }
        finally
        {
            // Exclusively created fixture root, never a supplied discovery/source/project directory.
            Directory.Delete(root, true);
        }

        Console.WriteLine(JsonSerializer.Serialize(new { Accepted = accepted, CompleteF2PerformanceProtocol = false }));
        return accepted ? 0 : 1;
    }

    private static async Task<bool> Session(string root, bool diagnostics)
    {
        string path = Path.Combine(root, "authored.wav");
        float[] source = Enumerable.Range(0, 44100).Select(i => (float)(0.02 * Math.Sin(i * 2 * Math.PI * 220 / 44100)))
            .ToArray();
        WriteSource(path, source);
        var document = ProjectDocument.Create();
        var initial = document.Current;
        byte[] before = ProjectPersistence.Encode(document);
        using var output = new WasapiOutput(diagnostics ? 110250 : 0, diagnostics: diagnostics);
        if (output.Facts is not { } facts || output.Failure != OutputFailure.None)
        {
            output.Close();
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Run = "audition-unavailable", Accepted = false, output.Failure,
                HResult = $"0x{output.HResult:X8}", output.Joined
            }));
            return false;
        }

        // Nested disposal must explicitly join the borrowing output before the audition is reclaimed.
        var audition = new WavAudition(facts.Rate, facts.Channels, facts.CapacityFrames, document);
        long stop = 0;
        bool stopAck = false, cancelled = false, replacement = false, closeStopped = false;
        bool naturalEof = false, canonicalBeforeClose = false;
        double oracle = 0;
        int initialResources = 0;
        try
        {
            output.Start(audition.Processor); // Silence until an explicit prepared preview is published.
            await Task.Delay(60);
            var discovery = await SourceAccess.DiscoverDirectoryAsync(root);
            initialResources = document.Current.State.Resources.Length;
            if (discovery.Sources.Count(item => item.Availability == SourceAvailability.Supported) != 1) return false;
            await Preview(audition, path);
            File.Delete(path);
            await Task.Delay(1250);
            audition.Processor.RetireCompleted();
            naturalEof = audition.Processor.ActiveVoices == 0 && audition.Processor.LivePreparedStates == 0;
            WriteSource(path, source);
            await Preview(audition, path);
            await Task.Delay(60);
            stop = audition.Stop();
            stopAck = await WaitStop(audition.Processor, stop);
            audition.Processor.RetireCompleted();
            await Preview(audition, path);
            await Task.Delay(60);
            await Preview(audition, path);
            await Task.Delay(60);
            audition.Processor.RetireCompleted();
            replacement = audition.Processor.ActiveVoices == 1;
            using var cancellation = new CancellationTokenSource();
            await Preview(audition, path, cancellation.Token);
            await Task.Delay(60);
            audition.Processor.RetireCompleted();
            cancellation.Cancel();
            await Task.Delay(60);
            audition.Processor.RetireCompleted();
            cancelled = audition.Processor.ActiveVoices == 0 && audition.Processor.LivePreparedStates == 0;
            await Preview(audition, path);
            await Task.Delay(60);
            long prior = audition.Processor.StopAcknowledgment;
            canonicalBeforeClose = before.SequenceEqual(ProjectPersistence.Encode(document));
            document.Close();
            await Task.Delay(60);
            audition.Processor.RetireCompleted();
            closeStopped = audition.Processor.ActiveVoices == 0 && audition.Processor.StopAcknowledgment > prior;
        }
        finally
        {
            output.Close(); // If join fails, do not release potentially borrowed PCM.
            audition.Dispose();
        }

        if (diagnostics) oracle = Compare(output, source, facts.Rate, facts.Channels);

        bool unchanged = canonicalBeforeClose && ReferenceEquals(initial, document.Current) &&
                         document.Current.State.Resources.Length == 0 && document.UndoCount == 0;
        bool accepted = output.Failure == OutputFailure.None && naturalEof && stopAck && cancelled && replacement &&
                        closeStopped &&
                        unchanged && audition.Processor.LivePreparedStates == 0 && initialResources == 0 &&
                        oracle <= 2e-6 &&
                        (!diagnostics || output.ServiceMisses == 0 && output.PacketMisses == 0 &&
                            output.ProcessorAllocatedBytes == 0 && output.NonFiniteSamples == 0) &&
                        (diagnostics || output.DiagnosticStorageBytes == 0 && output.Observations.IsEmpty &&
                            output.Capture.IsEmpty);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Run = "transient-audition", Accepted = accepted, Diagnostics = diagnostics,
            facts, output.DiagnosticStorageBytes, output.CallbackCount, output.ElapsedSeconds, output.Failure,
            HResult = $"0x{output.HResult:X8}",
            output.ServiceMisses, output.PacketMisses, output.PaddingExhaustions, output.ProcessorAllocatedBytes,
            output.ServiceAllocatedBytes,
            CaptureSamples = output.Capture.Length, IndependentOracleError = diagnostics ? (double?)oracle : null,
            NaturalEof = naturalEof, StopRequested = stop, StopAcknowledged = stopAck, Replacement = replacement,
            Cancelled = cancelled, CloseStopped = closeStopped, CanonicalUnchanged = unchanged,
            audition.Processor.PreparedStatesCreated,
            audition.Processor.PreparedStatesReleased, audition.Processor.LivePreparedStates,
            audition.Processor.TerminationBoundaries, output.Joined
        }));
        return accepted;
    }

    private static async Task Preview(WavAudition audition, string path, CancellationToken token = default)
    {
        using var request = audition.BeginExternal(path);
        await request.PrepareAsync(token);
        if (request.Publish(token) != SamplerPublication.Accepted)
            throw new InvalidOperationException("Preview publication refused.");
    }

    private static async Task<bool> WaitStop(RealtimeSampler processor, long command)
    {
        long start = Stopwatch.GetTimestamp();
        while (processor.StopAcknowledgment < command && Stopwatch.GetElapsedTime(start).TotalSeconds < 2)
            await Task.Delay(5);
        return processor.StopAcknowledgment >= command && processor.ActiveVoices == 0;
    }

    private static double Compare(WasapiOutput output, float[] source, int rate, int channels)
    {
        var capture = output.Capture;
        int first = 0;
        while (first < capture.Length && capture[first] == 0) first++;
        if (first == capture.Length) return double.PositiveInfinity;
        int start = first / channels - 1; // authored sample zero is silent, sample one is not
        int length = (int)Math.Ceiling(source.Length * (double)rate / 44100);
        if (start < 1 || capture.Length / channels < start + length + 1) return double.PositiveInfinity;
        double maximum = 0;
        for (int frame = 0; frame < capture.Length / channels; frame++)
        {
            double expected = 0;
            int age = frame - start;
            if (age >= 0 && age < length)
            {
                double cursor = age * 44100.0 / rate;
                int index = (int)cursor;
                expected = (source[index] + ((index + 1 < source.Length ? source[index + 1] : 0) - source[index]) *
                    (cursor - index)) * WavAudition.InitialGain;
            }

            for (int channel = 0; channel < channels; channel++)
                maximum = Math.Max(maximum, Math.Abs(capture[frame * channels + channel] - expected));
        }

        return maximum;
    }

    private static void WriteSource(string path, float[] source)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(36 + source.Length * 4);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((ushort)3);
        writer.Write((ushort)1);
        writer.Write(44100);
        writer.Write(176400);
        writer.Write((ushort)4);
        writer.Write((ushort)32);
        writer.Write("data"u8);
        writer.Write(source.Length * 4);
        foreach (float sample in source) writer.Write(sample);
    }
}
