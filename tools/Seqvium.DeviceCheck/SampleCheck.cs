// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Seqvium.Core;

/// <summary>Read-only library inspection and offline boundary evidence. Never opens a device.</summary>
internal static class SampleCheck
{
    internal static async Task<int> Run(string library, string output)
    {
        library = Path.GetFullPath(library); output = Path.GetFullPath(output);
        if (output.StartsWith(library + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || output == library)
            throw new ArgumentException("Verification output must be outside the source library.");
        Directory.CreateDirectory(output);
        var supported = new List<object>(); var rejected = new List<object>(); var boundaries = new List<object>();
        var files = Directory.EnumerateFiles(library, "*", SearchOption.AllDirectories).Take(4097).ToArray();
        if (files.Length > 4096) throw new IOException("Library inspection exceeds 4096 files.");
        int piano = 0, bass = 0;
        foreach (string path in files.Where(p => Path.GetExtension(p).Equals(".wav", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                await using var stream = File.OpenRead(path);
                using var pcm = WavDecoder.Decode(await WavDecoder.ReadBytesAsync(stream));
                supported.Add(new { File = Path.GetRelativePath(library, path), pcm.SampleRate, pcm.Channels, pcm.Frames });
                bool isPiano = path.Contains("Piano" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
                bool isBass = path.Contains("808 Bass", StringComparison.OrdinalIgnoreCase);
                if (isPiano && piano < 2 || isBass && bass < 2)
                { boundaries.Add(await CheckBoundary(path, output)); if (isPiano) piano++; else bass++; }
            }
            catch (WavFormatException error) { rejected.Add(new { File = Path.GetRelativePath(library, path), error.Message }); }
        }
        int flac = files.Count(p => Path.GetExtension(p).Equals(".flac", StringComparison.OrdinalIgnoreCase));
        var report = new { Passed = rejected.Count == 0, SupportedWav = supported.Count, RejectedWav = rejected.Count,
            UnsupportedFlac = flac, Supported = supported, Rejected = rejected, Boundaries = boundaries, PhysicalAudio = false };
        await File.WriteAllTextAsync(Path.Combine(output, "sample-check.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(JsonSerializer.Serialize(new { report.Passed, report.SupportedWav, report.RejectedWav, report.UnsupportedFlac, Boundaries = boundaries }));
        return rejected.Count == 0 ? 0 : 1;
    }

    private static async Task<object> CheckBoundary(string sourcePath, string output)
    {
        var document = ProjectDocument.Create();
        try
        {
            Id<Pattern> pattern = default;
            using var import = ProjectMedia.BeginImport(document, Path.GetFileNameWithoutExtension(sourcePath),
                releaseMilliseconds: 0, ownedMediaDirectory: Path.Combine(output, "media"));
            await import.PrepareAsync(sourcePath);
            import.Accept((edit, media) =>
            {
                pattern = edit.AddPattern("Boundary", new(3840000));
                var part = edit.AddPart(pattern, "Sample", media.SoundId);
                edit.AddNote(pattern, part, default, new(960000), 60, 1);
            });
            using var plan = SamplerPreparation.PreparePattern(document, pattern, 48000);
            using var raw = plan.CreateExecution(); using var smooth = plan.CreateExecution(smoothBoundaries: true);
            var a = new float[512]; var b = new float[512];
            float previousRaw = 0, previousSmooth = 0, rawBoundary = 0, smoothBoundary = 0;
            // Evaluate the same quarter-note gate used by Rack; include the first silent sample.
            while (raw.Position <= 24000)
            {
                int frames = (int)Math.Min(256, 24001 - raw.Position); long start = raw.Position;
                raw.Process(a.AsSpan(0, frames * 2)); smooth.Process(b.AsSpan(0, frames * 2));
                for (int i = 0; i < frames; i++)
                {
                    if (start + i is >= 23850 and <= 24000)
                    { rawBoundary = Math.Max(rawBoundary, Math.Abs(a[i * 2] - previousRaw)); smoothBoundary = Math.Max(smoothBoundary, Math.Abs(b[i * 2] - previousSmooth)); }
                    previousRaw = a[i * 2]; previousSmooth = b[i * 2];
                }
            }
            if (smoothBoundary > rawBoundary + .000001f) throw new IOException("Boundary smoothing increased the measured discontinuity.");
            return new { Sample = Path.GetFileName(sourcePath), GateSeconds = .5, RawMaximumDelta = rawBoundary,
                SmoothedMaximumDelta = smoothBoundary, SilentAfterGate = previousSmooth == 0, AcousticListening = false };
        }
        finally { document.Close(); }
    }
}
