// SPDX-License-Identifier: Apache-2.0
using System.Numerics;

namespace Seqvium.Core;

public readonly record struct MusicalPosition
{
    public const long TicksPerQuarter = 960_000;
    public long Ticks { get; }

    public MusicalPosition(long ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        Ticks = ticks;
    }

    public MusicalPosition Add(MusicalDuration duration) => new(checked(Ticks + duration.Ticks));
}

public readonly record struct MusicalDuration
{
    public long Ticks { get; }

    public MusicalDuration(long ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ticks);
        Ticks = ticks;
    }
}

public readonly record struct Tempo
{
    public decimal BeatsPerMinute { get; }
    internal long MicroBeatsPerMinute => checked((long)(BeatsPerMinute * 1_000_000m));

    public Tempo(decimal beatsPerMinute)
    {
        if (beatsPerMinute is < 1m or > 1000m || decimal.Round(beatsPerMinute, 6) != beatsPerMinute)
            throw new ArgumentOutOfRangeException(nameof(beatsPerMinute), "BPM must be 1–1000 with at most six fractional digits.");
        BeatsPerMinute = beatsPerMinute;
    }
}

/// <summary>Exact constant-tempo mapping. Convert absolute endpoints; do not accumulate rounded frames.</summary>
public static class ConstantTempoConversion
{
    public static long ToFrames(MusicalPosition position, Tempo tempo, int sampleRate)
    {
        Validate(tempo, sampleRate);
        return Round((BigInteger)position.Ticks * 60 * sampleRate * 1_000_000,
            (BigInteger)MusicalPosition.TicksPerQuarter * tempo.MicroBeatsPerMinute);
    }

    public static MusicalPosition FromFrames(long frames, Tempo tempo, int sampleRate)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frames);
        Validate(tempo, sampleRate);
        return new(Round((BigInteger)frames * MusicalPosition.TicksPerQuarter * tempo.MicroBeatsPerMinute,
            (BigInteger)60 * sampleRate * 1_000_000));
    }

    public static long DurationFrames(MusicalPosition start, MusicalDuration duration, Tempo tempo, int sampleRate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(duration.Ticks);
        return checked(ToFrames(start.Add(duration), tempo, sampleRate) - ToFrames(start, tempo, sampleRate));
    }

    // Nonnegative rational values: nearest integer, exact ties toward the later boundary.
    private static long Round(BigInteger numerator, BigInteger denominator)
    {
        var quotient = BigInteger.DivRem(numerator, denominator, out var remainder);
        return checked((long)(quotient + (remainder * 2 >= denominator ? 1 : 0)));
    }

    private static void Validate(Tempo tempo, int sampleRate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        _ = new Tempo(tempo.BeatsPerMinute); // Also reject default(Tempo).
    }
}
