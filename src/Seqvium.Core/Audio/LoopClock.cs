// SPDX-License-Identifier: Apache-2.0
namespace Seqvium.Core;

/// <summary>Exact absolute musical boundaries without BigInteger allocation on the consumer.</summary>
internal sealed class LoopClock(long startTicks, long lengthTicks, Tempo tempo, int sampleRate)
{
    private readonly Int128 _numerator = (Int128)60 * sampleRate * 1_000_000;
    private readonly Int128 _denominator = (Int128)MusicalPosition.TicksPerQuarter * tempo.MicroBeatsPerMinute;
    internal long StartTicks { get; } = startTicks;
    internal long LengthTicks { get; } = lengthTicks;
    internal long Frame(long ticks) => (long)(((Int128)ticks * _numerator + _denominator / 2) / _denominator);
    internal long Ticks(long frames) => (long)(((Int128)frames * _denominator + _numerator / 2) / _numerator);
    internal bool SpansFrame(long ticks) => (Int128)ticks * _numerator >= _denominator;
    internal bool Compatible(LoopClock other) => StartTicks == other.StartTicks && LengthTicks == other.LengthTicks &&
        _numerator == other._numerator && _denominator == other._denominator;
}
