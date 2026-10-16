// SPDX-License-Identifier: Apache-2.0

using System.Numerics;

namespace Seqvium.Core;

public readonly record struct BiquadCoefficients(double B0, double B1, double B2, double A1, double A2)
{
    public double Magnitude(double hz, int sampleRate)
    {
        var z = Complex.FromPolarCoordinates(1, -2 * Math.PI * hz / sampleRate);
        return ((B0 + B1 * z + B2 * z * z) / (1 + A1 * z + A2 * z * z)).Magnitude;
    }
}

public static class MixerDsp
{
    public static double Amplitude(decimal db) => db <= -90 ? 0 : Math.Pow(10, (double)db / 20);

    // Independently implemented bilinear biquad equations; coefficient meaning follows the W3C Audio EQ Cookbook.
    public static BiquadCoefficients[] Filters(MixerEq eq, int sampleRate) =>
    [
        eq.Enabled && eq.HighPassHz > 0
            ? Filter(0, (double)eq.HighPassHz, 0, Math.Sqrt(.5), sampleRate)
            : new(1, 0, 0, 0, 0),
        eq.Enabled ? Filter(1, 120, (double)eq.LowDb, 1, sampleRate) : new(1, 0, 0, 0, 0),
        eq.Enabled ? Filter(2, (double)eq.MidHz, (double)eq.MidDb, (double)eq.MidQ, sampleRate) : new(1, 0, 0, 0, 0),
        eq.Enabled ? Filter(3, 8000, (double)eq.HighDb, 1, sampleRate) : new(1, 0, 0, 0, 0)
    ];

    private static BiquadCoefficients Filter(int kind, double hz, double db, double q, int rate)
    {
        double w = 2 * Math.PI * hz / rate, c = Math.Cos(w), s = Math.Sin(w);
        double a = Math.Pow(10, db / 40), alpha = s / (2 * q), beta = Math.Sqrt(2 * a) * s;

        BiquadCoefficients Normalize(double b0, double b1, double b2, double a0, double a1, double a2) =>
            new(b0 / a0, b1 / a0, b2 / a0, a1 / a0, a2 / a0);

        return kind switch
        {
            0 => Normalize((1 + c) / 2, -(1 + c), (1 + c) / 2, 1 + alpha, -2 * c, 1 - alpha),
            2 => Normalize(1 + alpha * a, -2 * c, 1 - alpha * a, 1 + alpha / a, -2 * c, 1 - alpha / a),
            1 => Normalize(a * (a + 1 - (a - 1) * c + beta), 2 * a * (a - 1 - (a + 1) * c),
                a * (a + 1 - (a - 1) * c - beta), a + 1 + (a - 1) * c + beta,
                -2 * (a - 1 + (a + 1) * c), a + 1 + (a - 1) * c - beta),
            _ => Normalize(a * (a + 1 + (a - 1) * c + beta), -2 * a * (a - 1 + (a + 1) * c),
                a * (a + 1 + (a - 1) * c - beta), a + 1 - (a - 1) * c + beta,
                2 * (a - 1 - (a + 1) * c), a + 1 - (a - 1) * c - beta)
        };
    }

    public static double ReductionDb(double inputDb, MixerCompressor c)
    {
        double over = inputDb - (double)c.ThresholdDb, knee = (double)c.KneeDb, slope = 1 - 1 / (double)c.Ratio;
        return knee > 0 && Math.Abs(over) <= knee / 2 ? slope * Math.Pow(over + knee / 2, 2) / (2 * knee) :
            over > knee / 2 ? over * slope : 0;
    }
}

public readonly record struct MixerMeter(
    float PeakLeft,
    float PeakRight,
    float RmsLeft,
    float RmsRight,
    float ReductionDb,
    float HeldPeak,
    long Overloads,
    long NonFiniteSamples,
    long Frames);

/// <summary>Single audio-consumer DSP. All allocation and coefficients are prepared on the control owner.
/// Readers observe bounded value meters, without access to source leases or processor mutation.</summary>
internal sealed class MixerRuntime
{
    private sealed class Strip
    {
        internal readonly double[] Z1 = new double[8], Z2 = new double[8];
        internal BiquadCoefficients[] Filters;
        internal double Gain, Trim, Left, Right, Reduction;
        internal float Held;
        internal long Overloads, Invalid, Frames;
        internal MixerDelayDsp Delay;
        internal MixerReverbDsp? Reverb;

        internal Strip(PreparedMixerStrip p, int rate)
        {
            Delay = new(p.Delay, rate);
            if (p.Reverb.Enabled) Reverb = new(p.Reverb, rate);
            Filters = (BiquadCoefficients[])p.Filters.Clone();
            Gain = p.Gain;
            Trim = p.Trim;
            Left = p.Left;
            Right = p.Right;
        }
    }

    private PreparedMixer _plan;
    private readonly Strip[] _strips;
    private readonly double[] _left, _right, _energyLeft, _energyRight;
    private readonly float[] _peakLeft, _peakRight, _reduction;
    private readonly MixerMeter[] _meters;
    private readonly double _smoothing;
    private double _ceilingGain = 1;
    private int _sequence;
    private bool _automationStarted;

    internal MixerRuntime(PreparedMixer plan)
    {
        _plan = plan;
        _strips = plan.Strips.Select(p => new Strip(p, plan.SampleRate)).ToArray();
        int count = _strips.Length;
        _left = new double[count];
        _right = new double[count];
        _energyLeft = new double[count];
        _energyRight = new double[count];
        _peakLeft = new float[count];
        _peakRight = new float[count];
        _reduction = new float[count];
        _meters = new MixerMeter[count];
        _smoothing = 1 - Math.Exp(-1 / (.005 * plan.SampleRate));
    }

    internal void Update(PreparedMixer plan) => _plan = plan;

    internal void ContinueFrom(MixerRuntime old)
    {
        _automationStarted = old._automationStarted;
        for (int i = 0; i < _strips.Length; i++)
        {
            int previous = -1;
            for (int n = 0; n < old._strips.Length; n++)
                if (old._plan.Strips[n].Id == _plan.Strips[i].Id)
                {
                    previous = n;
                    break;
                }

            if (previous < 0) continue;
            var source = old._strips[previous];
            var target = _strips[i];
            // Old execution is retired immediately after continuation; transfer the sole consumer's line.
            (target.Delay, source.Delay) = (source.Delay, target.Delay);
            if (target.Reverb is { } reverb && source.Reverb is { } previousReverb)
                reverb.ContinueFrom(previousReverb);
            else if (target.Reverb is null && _plan.Strips[i].Reverb.Admitted)
                (target.Reverb, source.Reverb) = (source.Reverb, null);
            Array.Copy(source.Z1, target.Z1, 8);
            Array.Copy(source.Z2, target.Z2, 8);
            Array.Copy(source.Filters, target.Filters, 4);
            target.Gain = source.Gain;
            target.Trim = source.Trim;
            target.Left = source.Left;
            target.Right = source.Right;
            target.Reduction = source.Reduction;
            target.Held = source.Held;
            target.Overloads = source.Overloads;
            target.Invalid = source.Invalid;
            target.Frames = source.Frames;
        }

        _ceilingGain = old._ceilingGain;
    }

    internal void Reset()
    {
        for (int i = 0; i < _strips.Length; i++)
        {
            var strip = _strips[i];
            var prepared = _plan.Strips[i];
            Array.Clear(strip.Z1);
            Array.Clear(strip.Z2);
            strip.Reduction = 0;
            strip.Delay.Reset(prepared.Delay);
            strip.Reverb?.Reset(prepared.Reverb);
            Array.Copy(prepared.Filters, strip.Filters, 4);
            strip.Trim = prepared.Trim;
            strip.Gain = prepared.Gain;
            strip.Left = prepared.Left;
            strip.Right = prepared.Right;
        }

        _ceilingGain = 1;
        _automationStarted = false;
    }

    internal void ResetMeters()
    {
        Interlocked.Increment(ref _sequence);
        foreach (var strip in _strips)
        {
            strip.Held = 0;
            strip.Overloads = 0;
            strip.Invalid = 0;
            strip.Frames = 0;
        }

        Array.Clear(_meters);
        Interlocked.Increment(ref _sequence);
    }

    internal MixerMeter Read(Guid id)
    {
        int index = -1;
        for (int i = 0; i < _plan.Strips.Length; i++)
            if (_plan.Strips[i].Id == id)
            {
                index = i;
                break;
            }

        if (index < 0) return default;
        for (int retry = 0; retry < 8; retry++)
        {
            int sequence = Volatile.Read(ref _sequence);
            if ((sequence & 1) != 0) continue;
            var value = _meters[index];
            Thread.MemoryBarrier();
            if (sequence == Volatile.Read(ref _sequence)) return value;
        }

        return default;
    }

    internal void Process(float[][] signals, Span<float> output, int channels, long startFrame,
        PreparedAutomation?[] automation)
    {
        int frames = output.Length / channels;
        Array.Clear(_energyLeft);
        Array.Clear(_energyRight);
        Array.Clear(_peakLeft);
        Array.Clear(_peakRight);
        Array.Clear(_reduction);
        var plan = _plan;
        for (int frame = 0; frame < frames; frame++)
        {
            Array.Clear(_left);
            Array.Clear(_right);
            foreach (var tap in plan.Taps)
            {
                _left[tap.Channel] += signals[tap.Buffer][frame * channels] * tap.Gain;
                _right[tap.Channel] += signals[tap.Buffer][frame * channels + channels - 1] * tap.Gain;
            }

            for (int i = 0; i < _strips.Length; i++)
            {
                var p = plan.Strips[i];
                var s = _strips[i];
                double Smooth(double value, double target) => value + (target - value) * _smoothing;
                s.Trim = Smooth(s.Trim, p.Trim);
                double gain = i < automation.Length && automation[i] is { } curve
                    ? p.Audible ? curve.At(startFrame + frame) : 0
                    : p.Gain;
                s.Gain = !_automationStarted && frame == 0 && i < automation.Length && automation[i] is not null
                    ? gain
                    : Smooth(s.Gain, gain);
                s.Left = Smooth(s.Left, p.Left);
                s.Right = Smooth(s.Right, p.Right);
                double l = _left[i] * s.Trim, r = _right[i] * s.Trim;
                for (int band = 0; band < 4; band++)
                {
                    var old = s.Filters[band];
                    var next = p.Filters[band];
                    var c = old == next
                        ? next
                        : new BiquadCoefficients(Smooth(old.B0, next.B0), Smooth(old.B1, next.B1),
                            Smooth(old.B2, next.B2), Smooth(old.A1, next.A1), Smooth(old.A2, next.A2));
                    // Runtime coefficients must not mutate the prepared publication's arrays.
                    s.Filters[band] = c;

                    double Filter(double x, int n)
                    {
                        double y = c.B0 * x + s.Z1[n];
                        s.Z1[n] = c.B1 * x - c.A1 * y + s.Z2[n];
                        s.Z2[n] = c.B2 * x - c.A2 * y;
                        if (Math.Abs(s.Z1[n]) < 1e-25) s.Z1[n] = 0;
                        if (Math.Abs(s.Z2[n]) < 1e-25) s.Z2[n] = 0;
                        return y;
                    }

                    l = Filter(l, band * 2);
                    r = Filter(r, band * 2 + 1);
                }

                var comp = p.Settings.Compressor;
                double target = comp.Enabled
                    ? MixerDsp.ReductionDb(20 * Math.Log10(Math.Max(1e-12, Math.Max(Math.Abs(l), Math.Abs(r)))), comp)
                    : 0;
                double time = target > s.Reduction ? p.Attack : p.Release;
                s.Reduction = target + time * (s.Reduction - target);
                double compression = (s.Reduction < 1e-10 ? 1 : Math.Pow(10, -s.Reduction / 20)) * p.Makeup;
                l *= compression;
                r *= compression;
                s.Delay.Process(ref l, ref r, p.Delay);
                s.Reverb?.Process(ref l, ref r, p.Reverb);
                double preL = l * s.Left, preR = r * s.Right;
                l = preL * s.Gain;
                r = preR * s.Gain;
                if (!double.IsFinite(l) || !double.IsFinite(r) || Math.Abs(l) > float.MaxValue ||
                    Math.Abs(r) > float.MaxValue)
                {
                    l = r = preL = preR = 0;
                    s.Invalid++;
                    Array.Clear(s.Z1);
                    Array.Clear(s.Z2);
                    s.Reduction = 0;
                    s.Delay.Reset(p.Delay);
                    s.Reverb?.Reset(p.Reverb);
                }

                double peak = Math.Max(Math.Abs(l), Math.Abs(r));
                if (peak > 1) s.Overloads++;
                if (i == _strips.Length - 1 && plan.ProtectOutput)
                {
                    double desired = peak > .891250938 ? .891250938 / peak : 1;
                    _ceilingGain = desired < _ceilingGain
                        ? desired
                        : desired + Math.Exp(-1 / (.05 * plan.SampleRate)) * (_ceilingGain - desired);
                    l *= _ceilingGain;
                    r *= _ceilingGain;
                }

                _peakLeft[i] = Math.Max(_peakLeft[i], (float)Math.Abs(l));
                _peakRight[i] = Math.Max(_peakRight[i], (float)Math.Abs(r));
                _energyLeft[i] += l * l;
                _energyRight[i] += r * r;
                _reduction[i] = Math.Max(_reduction[i], (float)s.Reduction);
                if (p.Output >= 0)
                {
                    _left[p.Output] += l;
                    _right[p.Output] += r;
                }

                foreach (var send in p.Sends)
                {
                    _left[send.Target] += (send.PreFader && p.Audible ? preL : send.PreFader ? 0 : l) * send.Gain;
                    _right[send.Target] += (send.PreFader && p.Audible ? preR : send.PreFader ? 0 : r) * send.Gain;
                }

                if (i == _strips.Length - 1)
                {
                    output[frame * channels] = (float)(channels == 1 ? (l + r) / 2 : l);
                    if (channels == 2) output[frame * channels + 1] = (float)r;
                }
            }
        }

        _automationStarted = true;
        Interlocked.Increment(ref _sequence);
        for (int i = 0; i < _strips.Length; i++)
        {
            var s = _strips[i];
            s.Held = Math.Max(s.Held, Math.Max(_peakLeft[i], _peakRight[i]));
            s.Frames += frames;
            _meters[i] = new(_peakLeft[i], _peakRight[i], (float)Math.Sqrt(_energyLeft[i] / Math.Max(1, frames)),
                (float)Math.Sqrt(_energyRight[i] / Math.Max(1, frames)), _reduction[i], s.Held, s.Overloads, s.Invalid,
                s.Frames);
        }

        Interlocked.Increment(ref _sequence);
    }
}
