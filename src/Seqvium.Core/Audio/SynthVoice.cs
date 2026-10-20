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

    // Optional intent: never materialize this block merely by opening/saving an F1 instrument.
    public static ImmutableArray<Parameter> ExpressionParameters { get; } =
    [
        new("filterAttack", 0, 5000, 0, "ms"), new("filterDecay", 0, 5000, 180, "ms"),
        new("filterSustain", 0, 1, 0, "%"), new("filterRelease", 0, 8000, 180, "ms"),
        new("filterAmount", -8, 8, 0, "oct"), new("velocityCutoff", 0, 8, 0, "oct"),
        new("lfoTarget", 0, 2, 0, ""), new("lfoRate", .1m, 20, 5, "Hz"),
        new("lfoPitch", 0, 100, 0, "¢"), new("lfoCutoff", 0, 4, 0, "oct")
    ];

    public static decimal Value(ImmutableDictionary<string, decimal> values, string key) =>
        values.TryGetValue(key, out var value) ? value : ExpressionParameters.Single(p => p.Key == key).Default;

    public static bool HasExpressionIntent(SoundDefinition sound) => HasIntent(sound) &&
        ExpressionParameters.Any(p => sound.Parameters.ContainsKey(p.Key));

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
            .SetItem("release", 120).SetItem("cutoff", 4500).SetItem("resonance", .2m)),
        new("EvolvingPad", Defaults.SetItem("wave2", 1).SetItem("mix", .4m).SetItem("detune", 8)
            .SetItem("attack", 700).SetItem("release", 1800).SetItem("cutoff", 380)
            .SetItem("filterAttack", 1800).SetItem("filterDecay", 2400).SetItem("filterSustain", .2m)
            .SetItem("filterRelease", 1400).SetItem("filterAmount", 4)),
        new("ExpressiveLead", Defaults.SetItem("cutoff", 4200).SetItem("attack", 20).SetItem("release", 240)
            .SetItem("lfoTarget", 1).SetItem("lfoRate", 5.5m).SetItem("lfoPitch", 22)),
        new("VelocityPluck", Defaults.SetItem("attack", 2).SetItem("decay", 500).SetItem("sustain", 0)
            .SetItem("release", 140).SetItem("cutoff", 1300).SetItem("resonance", .2m)
            .SetItem("filterDecay", 160).SetItem("filterAmount", 3).SetItem("velocityCutoff", 4))
    ];

    public static bool HasIntent(SoundDefinition sound) => sound.Algorithm == Algorithm;

    public static bool Supports(SoundDefinition sound) => HasIntent(sound) && sound.Extension is null &&
                                                          sound.AdditionalData.IsEmpty && sound.ResourceIds.IsEmpty &&
                                                          sound.Parameters.Keys.All(key => Parameters.Any(p => p.Key == key) ||
                                                              ExpressionParameters.Any(p => p.Key == key)) &&
                                                          ExpressionParameters.All(p => !sound.Parameters.TryGetValue(p.Key, out var value) ||
                                                              value >= p.Minimum && value <= p.Maximum &&
                                                              (p.Key != "lfoTarget" || value == decimal.Truncate(value))) &&
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
            (double)p["sustain"], Frames("release"), g, k, 1 / (1 + g * (g + k)))
            { Modulation = SynthModulation.Prepare(p, rate) };
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
    internal SynthModulation Modulation { get; init; } = null!;
    internal double Velocity { get; init; } = 1;

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
    private double _lfoPhase, _lfoStep, _pitchDepth, _cutoffDepth, _filterAmount, _velocityDepth, _filterLevel;
    private double _filterReleaseLevel;
    private int _filterReleaseFrames;
    private bool _expressionStarted, _filterReleaseCaptured;

    private void FreezeFilterRelease(SynthModulation p, long age, int releaseAge)
    {
        if (releaseAge != 0) return;
        _filterReleaseLevel = _expressionStarted ? _filterLevel : p.Held(age);
        _filterReleaseFrames = p.Release;
        _filterReleaseCaptured = true;
    }

    private (double Pitch, double G) Modulate(PreparedSynth p, long age, int releaseAge, double g, int rate, bool live)
    {
        var m = p.Modulation;
        double smoothing = 1d / (.005 * rate);
        if (!_expressionStarted)
        {
            _lfoPhase = age * m.Rate % 1;
            _lfoStep = m.Rate;
            _pitchDepth = !live && m.Target == 1 ? m.Pitch : 0;
            _cutoffDepth = !live && m.Target == 2 ? m.Cutoff : 0;
            _filterAmount = live ? 0 : m.Amount;
            _velocityDepth = live ? 0 : m.Velocity;
            _filterLevel = m.Held(age);
            _expressionStarted = true;
        }
        if (live)
        {
            _lfoStep += (m.Rate - _lfoStep) * smoothing;
            _pitchDepth += ((m.Target == 1 ? m.Pitch : 0) - _pitchDepth) * smoothing;
            _cutoffDepth += ((m.Target == 2 ? m.Cutoff : 0) - _cutoffDepth) * smoothing;
            _filterAmount += (m.Amount - _filterAmount) * smoothing;
            _velocityDepth += (m.Velocity - _velocityDepth) * smoothing;
        }
        if (releaseAge < 0) _filterLevel = live ? _filterLevel + (m.Held(age) - _filterLevel) * smoothing : m.Held(age);
        else
        {
            // Cold seek reconstructs the NoteOff level and duration without historical filter preroll.
            if (!_filterReleaseCaptured && releaseAge > 0)
            {
                _filterReleaseLevel = m.Held(age - releaseAge);
                _filterReleaseFrames = m.Release;
                _filterReleaseCaptured = true;
            }
            _filterLevel = _filterReleaseFrames == 0 ? 0 :
                _filterReleaseLevel * Math.Max(0, 1 - (double)releaseAge / _filterReleaseFrames);
        }
        double lfo = m.Lookup.Sine(_lfoPhase);
        _lfoPhase += _lfoStep;
        if (_lfoPhase >= 1) _lfoPhase -= 1;
        double octaves = _filterAmount * _filterLevel + _velocityDepth * (p.Velocity - 1) + _cutoffDepth * lfo;
        double frequency = g == p.G ? m.BaseCutoff : m.Lookup.Hertz(g);
        return (_pitchDepth == 0 ? 1 : SynthModulation.PitchRatio(_pitchDepth * lfo),
            octaves == 0 ? g : m.Lookup.Coefficient(frequency * m.Lookup.Ratio(octaves)));
    }

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
        double pitchAge = p.Modulation.ElapsedPitchFrames(age);
        _phase1 = pitchAge * p.Step1 % 1;
        _phase2 = pitchAge * p.Step2 % 1;
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
        _expressionStarted = false;
        _filterReleaseFrames = 0;
        _filterReleaseCaptured = false;
    }

    internal double Next(PreparedSynth p, long age, int releaseAge, double g, int rate)
    {
        if (releaseAge == 0) _releaseLevel = p.HeldEnvelope(age);
        _level = p.Envelope(age, releaseAge);
        if (releaseAge == 0) _filterLevel = p.Modulation.Held(age);
        FreezeFilterRelease(p.Modulation, age, releaseAge);
        double step1 = p.Step1, step2 = p.Step2;
        if (p.Modulation.Active)
        {
            var modulation = Modulate(p, age, releaseAge, g, rate, false);
            step1 *= modulation.Pitch; step2 *= modulation.Pitch; g = modulation.G;
        }
        double input = (1 - p.Mix) * SynthVoice.Oscillator(p.Wave1, _phase1, step1) +
                       p.Mix * SynthVoice.Oscillator(p.Wave2, _phase2, step2);
        _phase1 += step1;
        _phase1 -= Math.Floor(_phase1);
        _phase2 += step2;
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
        FreezeFilterRelease(p.Modulation, age, releaseAge);
        double step1 = _step1, step2 = _step2, g = p.G;
        if (p.Modulation.Active || _expressionStarted)
        {
            var modulation = Modulate(p, age, releaseAge, g, rate, true);
            step1 *= modulation.Pitch; step2 *= modulation.Pitch; g = modulation.G;
        }
        double input = (1 - _mix) * _wave1.Next(p.Wave1, _phase1, step1, smoothing) +
                       _mix * _wave2.Next(p.Wave2, _phase2, step2, smoothing);
        _phase1 += step1;
        _phase1 -= Math.Floor(_phase1);
        _phase2 += step2;
        _phase2 -= Math.Floor(_phase2);
        _g = double.IsNaN(_g) ? g : _g + (g - _g) * smoothing;
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
