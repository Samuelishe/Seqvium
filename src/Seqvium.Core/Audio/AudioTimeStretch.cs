// SPDX-License-Identifier: Apache-2.0

namespace Seqvium.Core;

/// <summary>Worker-only, stereo-linked WSOLA for sustained/monophonic musical phrases.
/// A 40-ms frame, 20-ms synthesis hop and ±10-ms waveform search retain local pitch.
/// This is independent host code; no third-party implementation or native runtime is bundled.</summary>
internal static class AudioTimeStretch
{
    internal const long MaximumDerivedBytes = 128L * 1024 * 1024;
    internal const int MaximumSourceSeconds = 60;

    internal static long RequiredBytes(long frames, int rate, int channels, decimal ratio)
    {
        if (ratio is < .5m or > 2m || frames < rate / 10 || frames > (long)rate * MaximumSourceSeconds)
            throw new GraphPreparationException("AudioClip.StretchRange");
        long bytes = checked(AudioClipSource.StretchedFrames(frames, ratio) * channels * sizeof(float));
        if (bytes > MaximumDerivedBytes) throw new GraphPreparationException(GraphPreparationReasons.Budget);
        return bytes;
    }

    internal static PcmLease Prepare(PcmLease source, long offset, long frames, decimal ratio,
        CancellationToken cancellation)
    {
        long bytes = RequiredBytes(frames, source.Rate, source.Channels, ratio);
        cancellation.ThrowIfCancellationRequested();
        int count = checked((int)frames), channels = source.Channels;
        int target = checked((int)(bytes / (channels * sizeof(float))));
        var input = source.Samples;
        var output = new float[target * channels];
        int window = source.Rate * 40 / 1000;
        int hop = window / 2, search = source.Rate / 100;
        int stride = Math.Max(1, source.Rate / 12000);
        int sourceOffset = checked((int)offset);
        int previous = 0, lastOutput = 0;
        // Anchor both endpoints. The final full frame ends at the original EOF; no appended silence,
        // fixed-rate playback slowdown or tail outside the canonical duration is introduced.
        Copy(0, 0, window);
        for (int destination = hop; lastOutput < target - window; destination += hop)
        {
            cancellation.ThrowIfCancellationRequested();
            destination = Math.Min(destination, target - window);
            int advance = destination - lastOutput;
            int nominal = (int)Math.Round((double)destination * (count - window) / (target - window));
            int low = Math.Max(0, nominal - search), high = Math.Min(count - window, nominal + search);
            int chosen = nominal;
            if (destination != target - window)
            {
                double best = double.NegativeInfinity;
                int overlap = window - advance;
                for (int candidate = low; candidate <= high; candidate += stride)
                    Consider(candidate);
                int coarse = chosen;
                for (int candidate = Math.Max(low, coarse - stride); candidate <= Math.Min(high, coarse + stride); candidate++)
                    Consider(candidate);

                void Consider(int candidate)
                {
                    // Sum channel-wise dot products rather than a mono sum: anti-phase stereo must
                    // not cancel the search signal. One offset always serves both channels.
                    double dot = 0, aEnergy = 0, bEnergy = 0;
                    for (int i = 0; i < overlap; i += stride)
                    for (int c = 0; c < channels; c++)
                    {
                        double a = input[(sourceOffset + previous + advance + i) * channels + c];
                        double b = input[(sourceOffset + candidate + i) * channels + c];
                        dot += a * b; aEnergy += a * a; bEnergy += b * b;
                    }
                    double score = dot / Math.Sqrt(Math.Max(1e-30, aEnergy * bEnergy));
                    // Deterministic near-nominal tie break for silence/periodic waveforms.
                    if (score > best + 1e-10 || Math.Abs(score - best) <= 1e-10 &&
                        Math.Abs(candidate - nominal) < Math.Abs(chosen - nominal))
                    { best = score; chosen = candidate; }
                }
            }
            int blended = window - advance;
            for (int i = 0; i < blended; i++)
            {
                double weight = .5 - .5 * Math.Cos(Math.PI * (i + 1d) / (blended + 1d));
                for (int c = 0; c < channels; c++)
                {
                    int index = (destination + i) * channels + c;
                    output[index] = (float)(output[index] * (1 - weight) +
                        input[(sourceOffset + chosen + i) * channels + c] * weight);
                }
            }
            Copy(chosen + blended, destination + blended, advance);
            previous = chosen;
            lastOutput = destination;
        }
        cancellation.ThrowIfCancellationRequested();
        return new(source.Rate, channels, output);

        void Copy(int from, int to, int length) => Array.Copy(input, (sourceOffset + from) * channels,
            output, to * channels, length * channels);
    }
}
