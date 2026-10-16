// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

/// <summary>Versioned, media-free subtractive sound. Runtime phase/filter state belongs to each voice slot.</summary>
public static class SynthVoice
{
    public const string Algorithm = "core.subtractive-synth.polyblep-v1";

    public enum Wave
    {
        Sine,
        Triangle,
        Saw,
        Pulse
    }

    public sealed record Parameter(string Key, decimal Minimum, decimal Maximum, decimal Default, string Unit);

    public static ImmutableArray<Parameter> Parameters { get; } =
    [
        new("wave1", 0, 3, 2, ""), new("wave2", 0, 3, 0, ""),
        new("mix", 0, 1, .3m, "%"), new("detune", -100, 100, 5, "¢"),
        new("attack", 0, 5000, 8, "ms"), new("decay", 0, 5000, 180, "ms"),
        new("sustain", 0, 1, .65m, "%"), new("release", 0, 8000, 180, "ms"),
        new("cutoff", 20, 18000, 1800, "Hz"), new("resonance", 0, 1, .12m, "%")
    ];

    public static ImmutableDictionary<string, decimal> Defaults { get; } =
        Parameters.ToImmutableDictionary(p => p.Key, p => p.Default);

    public sealed record Preset(string Key, ImmutableDictionary<string, decimal> Values);

    public static ImmutableArray<Preset> Presets { get; } =
    [
        new("WarmBass",
            Defaults.SetItem("cutoff", 750).SetItem("mix", .25m).SetItem("sustain", .7m).SetItem("release", 100)),
        new("SoftPad", Defaults.SetItem("wave1", 1).SetItem("wave2", 2).SetItem("mix", .35m).SetItem("detune", 9)
            .SetItem("attack", 650).SetItem("decay", 900).SetItem("sustain", .75m).SetItem("release", 1600)
            .SetItem("cutoff", 2400)),
        new("BrightLead", Defaults.SetItem("wave2", 3).SetItem("mix", .4m).SetItem("cutoff", 7500)
            .SetItem("resonance", .3m).SetItem("release", 220)),
        new("Pluck", Defaults.SetItem("attack", 2).SetItem("decay", 280).SetItem("sustain", 0)
            .SetItem("release", 120).SetItem("cutoff", 4500).SetItem("resonance", .2m))
    ];

    public static bool HasIntent(SoundDefinition sound) => sound.Algorithm == Algorithm;

    public static bool Supports(SoundDefinition sound) => HasIntent(sound) && sound.Extension is null &&
                                                          sound.AdditionalData.IsEmpty && sound.ResourceIds.IsEmpty &&
                                                          sound.Parameters.Count == Parameters.Length &&
                                                          Parameters.All(p =>
                                                              sound.Parameters.TryGetValue(p.Key, out var value) &&
                                                              value >= p.Minimum && value <= p.Maximum &&
                                                              (!p.Key.StartsWith("wave", StringComparison.Ordinal) ||
                                                               value == decimal.Truncate(value)));

    public static void Validate(ImmutableDictionary<string, decimal> values)
    {
        if (!Supports(new(default, "Synth", Algorithm, values, [], null)))
            throw new NotSupportedException("Unsupported subtractive synth state or parameter range.");
    }

    public static double Frequency(decimal pitch) => 440 * Math.Pow(2, (double)(pitch - 69) / 12);

    internal static PreparedSynth Prepare(SoundDefinition sound, int rate, decimal pitch)
    {
        if (!Supports(sound) || rate is not (44100 or 48000) || pitch is < 0 or > 127)
            throw new NotSupportedException("Unsupported synth sound, rate or pitch.");
        var p = sound.Parameters;
        int Frames(string key) => (int)decimal.Round(p[key] * rate / 1000, 0, MidpointRounding.AwayFromZero);
        double g = Math.Tan(Math.PI * (double)p["cutoff"] / rate);
        double k = 2 - 1.9 * (double)p["resonance"];
        return new((Wave)(int)p["wave1"], (Wave)(int)p["wave2"], Frequency(pitch) / rate,
            Frequency(pitch + p["detune"] / 100) / rate, (double)p["mix"], Frames("attack"), Frames("decay"),
            (double)p["sustain"], Frames("release"), g, k, 1 / (1 + g * (g + k)));
    }

    // Polynomial discontinuity corrections are evaluated in normalized oscillator time.
    // Triangle uses the integral of the step correction (BLAMP) at its two slope changes.
    internal static double Oscillator(Wave wave, double phase, double step)
    {
        static double Blep(double t, double d)
        {
            if (t < d)
            {
                double x = t / d;
                return 2 * x - x * x - 1;
            }

            if (t > 1 - d)
            {
                double x = (t - 1) / d;
                return x * x + 2 * x + 1;
            }

            return 0;
        }

        static double Blamp(double t, double d)
        {
            if (t < d)
            {
                double x = t / d - 1;
                return -x * x * x / 3;
            }

            if (t > 1 - d)
            {
                double x = (t - 1) / d + 1;
                return x * x * x / 3;
            }

            return 0;
        }

        double half = phase < .5 ? phase + .5 : phase - .5;
        return wave switch
        {
            Wave.Sine => Math.Sin(2 * Math.PI * phase),
            Wave.Saw => 2 * phase - 1 - Blep(phase, step),
            Wave.Pulse => (phase < .5 ? 1 : -1) + Blep(phase, step) - Blep(half, step),
            Wave.Triangle => 1 - 4 * Math.Abs(phase - .5) + 4 * step * (Blamp(phase, step) - Blamp(half, step)),
            _ => 0 // Preparation validates the closed waveform range.
        };
    }
}

internal sealed record PreparedSynth(
    SynthVoice.Wave Wave1,
    SynthVoice.Wave Wave2,
    double Step1,
    double Step2,
    double Mix,
    int Attack,
    int Decay,
    double Sustain,
    int Release,
    double G,
    double K,
    double IntegratorGain)
{
    internal double HeldEnvelope(long age) => Attack > 0 && age < Attack ? (double)age / Attack :
        Decay > 0 && age - Attack < Decay ? 1 + (Sustain - 1) * (age - Attack) / Decay : Sustain;

    internal double Envelope(long age, int releaseAge) => releaseAge < 0 ? HeldEnvelope(age) :
        Release == 0 ? 0 : HeldEnvelope(age - releaseAge) * Math.Max(0, 1 - (double)releaseAge / Release);
}

internal struct SynthState
{
    private double _phase1, _phase2, _ic1, _ic2, _g;
    private double _mix, _step1, _step2, _k, _level, _releaseLevel;
    private WaveBlend _wave1, _wave2;

    // Inline fixed-size weights allow a rapid reversal to continue its current crossfade.
    private struct WaveBlend
    {
        private double _sine, _triangle, _saw, _pulse;

        internal WaveBlend(SynthVoice.Wave wave)
        {
            _sine = wave == SynthVoice.Wave.Sine ? 1 : 0;
            _triangle = wave == SynthVoice.Wave.Triangle ? 1 : 0;
            _saw = wave == SynthVoice.Wave.Saw ? 1 : 0;
            _pulse = wave == SynthVoice.Wave.Pulse ? 1 : 0;
        }

        internal double Next(SynthVoice.Wave wave, double phase, double step, double smoothing)
        {
            _sine += ((wave == SynthVoice.Wave.Sine ? 1 : 0) - _sine) * smoothing;
            _triangle += ((wave == SynthVoice.Wave.Triangle ? 1 : 0) - _triangle) * smoothing;
            _saw += ((wave == SynthVoice.Wave.Saw ? 1 : 0) - _saw) * smoothing;
            _pulse += ((wave == SynthVoice.Wave.Pulse ? 1 : 0) - _pulse) * smoothing;
            // Avoid an indefinitely decaying subnormal weight after a shape has settled.
            if (_sine < 1e-12) _sine = 0;
            if (_triangle < 1e-12) _triangle = 0;
            if (_saw < 1e-12) _saw = 0;
            if (_pulse < 1e-12) _pulse = 0;
            return _sine * SynthVoice.Oscillator(SynthVoice.Wave.Sine, phase, step) +
                   _triangle * SynthVoice.Oscillator(SynthVoice.Wave.Triangle, phase, step) +
                   _saw * SynthVoice.Oscillator(SynthVoice.Wave.Saw, phase, step) +
                   _pulse * SynthVoice.Oscillator(SynthVoice.Wave.Pulse, phase, step);
        }
    }

    internal void Reset(PreparedSynth p, long age = 0)
    {
        _phase1 = age * p.Step1 % 1;
        _phase2 = age * p.Step2 % 1;
        _ic1 = _ic2 = 0;
        _g = double.NaN;
        _mix = p.Mix;
        _step1 = p.Step1;
        _step2 = p.Step2;
        _k = p.K;
        _wave1 = new(p.Wave1);
        _wave2 = new(p.Wave2);
        _level = p.HeldEnvelope(age);
        _releaseLevel = 0;
    }

    internal double Next(PreparedSynth p, long age, int releaseAge, double g, int rate)
    {
        if (releaseAge == 0) _releaseLevel = p.HeldEnvelope(age);
        _level = p.Envelope(age, releaseAge);
        double input = (1 - p.Mix) * SynthVoice.Oscillator(p.Wave1, _phase1, p.Step1) +
                       p.Mix * SynthVoice.Oscillator(p.Wave2, _phase2, p.Step2);
        _phase1 += p.Step1;
        _phase1 -= Math.Floor(_phase1);
        _phase2 += p.Step2;
        _phase2 -= Math.Floor(_phase2);
        // Trapezoidal state-variable low-pass: positive damping, bounded cutoff below Nyquist.
        // Solve both coupled integrators together; automation supplies a worker-built coefficient lookup.
        double v3 = input - _ic2;
        // Smooth live curve replacement without touching phase/envelope or filter integrators.
        _g = double.IsNaN(_g) ? g : _g + (g - _g) * (1d / (.005 * rate));
        double integratorGain = _g == p.G ? p.IntegratorGain : 1 / (1 + _g * (_g + p.K));
        double v1 = (_ic1 + _g * v3) * integratorGain;
        double v2 = _ic2 + _g * v1;
        _ic1 = 2 * v1 - _ic1;
        _ic2 = 2 * v2 - _ic2;
        return .18 * v2 * _level;
    }

    internal double NextLive(PreparedSynth p, long age, int releaseAge, int releaseFrames, int rate)
    {
        double smoothing = 1d / (.005 * rate);
        _mix += (p.Mix - _mix) * smoothing;
        _step1 += (p.Step1 - _step1) * smoothing;
        _step2 += (p.Step2 - _step2) * smoothing;
        _k += (p.K - _k) * smoothing;
        double input = (1 - _mix) * _wave1.Next(p.Wave1, _phase1, _step1, smoothing) +
                       _mix * _wave2.Next(p.Wave2, _phase2, _step2, smoothing);
        _phase1 += _step1;
        _phase1 -= Math.Floor(_phase1);
        _phase2 += _step2;
        _phase2 -= Math.Floor(_phase2);
        _g = double.IsNaN(_g) ? p.G : _g + (p.G - _g) * smoothing;
        double v1 = (_ic1 + _g * (input - _ic2)) / (1 + _g * (_g + _k));
        double v2 = _ic2 + _g * v1;
        _ic1 = 2 * v1 - _ic1;
        _ic2 = 2 * v2 - _ic2;
        if (releaseAge < 0) _level += (p.HeldEnvelope(age) - _level) * smoothing;
        else
        {
            if (releaseAge == 0) _releaseLevel = _level;
            _level = releaseFrames == 0 ? 0 : _releaseLevel * Math.Max(0, 1 - (double)releaseAge / releaseFrames);
        }

        return .18 * v2 * _level;
    }
}