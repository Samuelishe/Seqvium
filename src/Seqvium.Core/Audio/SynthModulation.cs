// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;

namespace Seqvium.Core;

/// <summary>Immutable per-note expression and shared host-rate lookups, built on the preparation owner.</summary>
internal sealed record SynthModulation(int Attack, int Decay, double Sustain, int Release, double Amount,
    double Velocity, int Target, double Rate, double Pitch, double Cutoff, double BaseCutoff, SynthModulation.Tables Lookup)
{
    // Value equality matters: unchanged prepared notes must survive live publication.
    private readonly record struct PitchPhase(double Mean, double First, double Second, double Third,
        double Fourth, double Fifth, double Sixth)
    {
        internal double this[int harmonic] => harmonic switch
        { 0 => Mean, 1 => First, 2 => Second, 3 => Third, 4 => Fourth, 5 => Fifth, _ => Sixth };
    }
    private PitchPhase? PitchTravel { get; init; }
    internal bool Active => Amount != 0 || Velocity != 0 || Target == 1 && Pitch != 0 || Target == 2 && Cutoff != 0;

    internal double Held(long age) => Attack > 0 && age < Attack ? (double)age / Attack :
        Decay > 0 && age - Attack < Decay ? 1 + (Sustain - 1) * (age - Attack) / Decay : Sustain;

    internal static SynthModulation Prepare(ImmutableDictionary<string, decimal> values, int rate)
    {
        double V(string key) => (double)SynthVoice.Value(values, key);
        int Frames(string key) => (int)Math.Round(V(key) * rate / 1000, MidpointRounding.AwayFromZero);
        var prepared = new SynthModulation(Frames("filterAttack"), Frames("filterDecay"), V("filterSustain"), Frames("filterRelease"),
            V("filterAmount"), V("velocityCutoff"), (int)V("lfoTarget"), V("lfoRate") / rate,
            V("lfoPitch") / 1200, V("lfoCutoff"), (double)values["cutoff"], Tables.ForRate(rate));
        if (prepared.Target != 1 || prepared.Pitch == 0) return prepared;
        // Six harmonics suffice at <=100 cents (|ln(2)*depth| <= .058). Derive on the worker.
        // The discrete sum restores carrier phase at cold seek without replaying elapsed samples.
        double half = prepared.Pitch * Math.Log(2) / 2;
        Span<double> travel = stackalloc double[7];
        double leading = 1;
        for (int harmonic = 0; harmonic <= 6; harmonic++)
        {
            if (harmonic > 0) leading *= half / harmonic;
            double term = leading, sum = term;
            for (int j = 1; j <= 6; j++) { term *= half * half / (j * (j + harmonic)); sum += term; }
            travel[harmonic] = harmonic == 0 ? sum : 2 * sum / Math.Sin(harmonic * Math.PI * prepared.Rate);
        }
        return prepared with { PitchTravel = new(travel[0], travel[1], travel[2], travel[3], travel[4], travel[5], travel[6]) };
    }

    internal double ElapsedPitchFrames(long age)
    {
        if (age == 0 || PitchTravel is not { } travel) return age;
        double sum = age * travel[0];
        for (int harmonic = 1; harmonic <= 6; harmonic++)
        {
            double step = harmonic * Math.PI * Rate;
            double wave = harmonic % 2 == 1 ? Math.Sin((age - 1) * step) : Math.Cos((age - 1) * step);
            sum += (harmonic % 4 is 0 or 1 ? 1 : -1) * travel[harmonic] * Math.Sin(age * step) * wave;
        }
        return sum;
    }

    internal static double PitchRatio(double octaves)
    {
        double x = octaves * .6931471805599453;
        return 1 + x * (1 + x * (.5 + x * (1d / 6 + x * (1d / 24 + x * (1d / 120 + x / 720)))));
    }

    internal sealed class Tables
    {
        private const int Size = 8192, RatioSize = 32768;
        private static readonly Tables Rate44100 = new(44100), Rate48000 = new(48000);
        private readonly double[] _sine = new double[Size + 1], _ratio = new double[RatioSize + 1];
        private readonly double[] _g = new double[Size + 1], _hz = new double[Size + 1];
        private readonly double _maximumG;
        internal const int StorageBytes = (3 * (Size + 1) + RatioSize + 1) * sizeof(double);
        private Tables(int rate)
        {
            _maximumG = Math.Tan(Math.PI * 18000 / rate);
            for (int i = 0; i <= Size; i++)
            {
                _sine[i] = Math.Sin(2 * Math.PI * i / Size);
                _g[i] = Math.Tan(Math.PI * (20 + 17980d * i / Size) / rate);
                _hz[i] = Math.Atan(_maximumG * i / Size) * rate / Math.PI;
            }
            for (int i = 0; i <= RatioSize; i++) _ratio[i] = Math.Pow(2, -20 + 40d * i / RatioSize);
        }

        internal static Tables ForRate(int rate) => rate == 44100 ? Rate44100 : Rate48000;
        private static double At(double[] table, double index)
        {
            int first = Math.Min(table.Length - 2, (int)index);
            return table[first] + (table[first + 1] - table[first]) * (index - first);
        }
        internal double Sine(double phase) => At(_sine, phase * Size);
        internal double Ratio(double octaves) => At(_ratio, (Math.Clamp(octaves, -20, 20) + 20) * (RatioSize / 40d));
        internal double Hertz(double g) => At(_hz, Math.Clamp(g / _maximumG, 0, 1) * Size);
        internal double Coefficient(double hertz) => At(_g, (Math.Clamp(hertz, 20, 18000) - 20) * (Size / 17980d));
    }
}
