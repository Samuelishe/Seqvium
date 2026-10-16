// SPDX-License-Identifier: Apache-2.0

namespace Seqvium.Core;

internal sealed record PreparedReverb(bool Enabled, bool Admitted, decimal Size, int[] Lengths, double[] Gains,
    double Damping, double Mix, long TailFrames);

/// <summary>Eight-line normalized Hadamard FDN with stereo allpass input diffusion. Each prepared
/// execution owns its storage; publication transfers references, never copies or frees line payloads.</summary>
internal sealed class MixerReverbDsp
{
    internal const int MaximumBytes = 16 * 1024 * 1024;
    private static readonly double[] Times = [.0297, .0371, .0419, .0437, .0479, .0533, .0593, .0677,
        .0017, .0043, .0077, .0113, .0019, .0047, .0083, .0121];
    private const double Normalize = .3535533905932738;

    internal static int StorageBytes(int rate) => checked(2 * Times.Sum(t =>
        ((int)Math.Round(t * 1.5 * rate) + 1) * (sizeof(float) + sizeof(long))));

    internal static PreparedReverb Prepare(MixerReverb r, int rate)
    {
        int[] lengths = Times.Select(t => Math.Max(1, (int)Math.Round(t * (.5 + (double)r.Size) * rate) | 1)).ToArray();
        return new(r.Enabled, r.Enabled, r.Size, lengths,
            lengths.Take(8).Select(n => Math.Pow(.001, n / ((double)r.DecaySeconds * rate))).ToArray(),
            Math.Exp(-2 * Math.PI * (16000 * Math.Pow(.025, (double)r.Damping)) / rate),
            (double)r.Mix, r.Enabled && r.Mix > 0 ? (long)Math.Ceiling((2 * (double)r.DecaySeconds + 1) * rate) : 0);
    }

    private sealed class Bank
    {
        private readonly float[][] _lines;
        private readonly long[][] _tags;
        private readonly int[] _positions = new int[16];
        private readonly double[] _filtered = new double[8], _gains;
        private readonly double _smoothing;
        private double _damping;
        private long _generation = 1;
        internal PreparedReverb Settings;
        internal readonly decimal Size;

        internal Bank(PreparedReverb p, int rate)
        {
            Settings = p;
            Size = p.Size;
            _lines = p.Lengths.Select(n => new float[n]).ToArray();
            _tags = p.Lengths.Select(n => new long[n]).ToArray();
            _gains = (double[])p.Gains.Clone();
            _damping = p.Damping;
            _smoothing = 1 - Math.Exp(-1 / (.02 * rate));
        }

        internal void Reset()
        {
            _generation++;
            Array.Clear(_filtered);
            Array.Copy(Settings.Gains, _gains, 8);
            _damping = Settings.Damping;
        }

        private double Read(int n) => _tags[n][_positions[n]] == _generation ? _lines[n][_positions[n]] : 0;
        private void Write(int n, double value)
        {
            _lines[n][_positions[n]] = (float)value;
            _tags[n][_positions[n]] = _generation;
            if (++_positions[n] == _lines[n].Length) _positions[n] = 0;
        }

        private double Diffuse(double input, int start)
        {
            for (int i = start; i < start + 4; i++)
            {
                double delayed = Read(i), output = delayed - .6 * input;
                Write(i, input + .6 * output);
                input = output;
            }
            return input;
        }

        internal void Process(double left, double right, out double wetL, out double wetR)
        {
            left = Diffuse(left, 8);
            right = Diffuse(right, 12);
            Span<double> values = stackalloc double[8];
            _damping += (Settings.Damping - _damping) * _smoothing;
            wetL = wetR = 0;
            for (int i = 0; i < 8; i++)
            {
                double value = Read(i);
                _filtered[i] = value * (1 - _damping) + _filtered[i] * _damping;
                if (Math.Abs(_filtered[i]) < 1e-25) _filtered[i] = 0;
                _gains[i] += (Settings.Gains[i] - _gains[i]) * _smoothing;
                values[i] = _filtered[i] * _gains[i];
                wetL += _filtered[i] * ((i & 1) == 0 ? Normalize : -Normalize);
                wetR += _filtered[i] * ((i & 2) == 0 ? Normalize : -Normalize);
            }
            // Orthogonal scattering conserves network energy before strictly sub-unity losses.
            for (int step = 1; step < 8; step *= 2)
                for (int group = 0; group < 8; group += step * 2)
                    for (int j = group; j < group + step; j++)
                    {
                        double a = values[j], b = values[j + step];
                        values[j] = a + b;
                        values[j + step] = a - b;
                    }
            for (int i = 0; i < 8; i++)
                Write(i, values[i] * Normalize + .25 *
                    (left * ((i & 4) == 0 ? 1 : -1) + right * ((i & 3) is 0 or 3 ? 1 : -1)));
        }
    }

    private Bank _bank;
    private Bank? _fading;
    private readonly int _rate, _transitionFrames;
    private readonly double _smoothing;
    private int _transition;
    private long _remaining, _tail;
    private double _mix;
    private bool _bypassed;

    internal MixerReverbDsp(PreparedReverb settings, int rate)
    {
        _rate = rate;
        _transitionFrames = Math.Max(1, rate * 30 / 1000);
        _smoothing = 1 - Math.Exp(-1 / (.005 * rate));
        _bank = new(settings, rate);
        Reset(settings);
    }

    internal void Reset(PreparedReverb p)
    {
        _bank.Settings = p;
        _bank.Reset();
        _fading?.Reset();
        _transition = 0;
        _remaining = 0;
        _tail = p.TailFrames;
        _mix = p.Enabled ? p.Mix : 0;
        _bypassed = !p.Enabled;
    }

    internal void ContinueFrom(MixerReverbDsp old)
    {
        if (_bank.Size == old._bank.Size)
        {
            (_bank, old._bank) = (old._bank, _bank);
            (_fading, old._fading) = (old._fading, _fading);
            _transition = old._transition;
        }
        else
        {
            // The new geometry was allocated by preparation. Keep one old bank for a 30-ms crossfade.
            (_fading, old._bank) = (old._bank, _bank);
            _transition = _transitionFrames;
        }
        _mix = old._mix;
        _remaining = old._remaining;
        _tail = Math.Max(_tail, old._tail);
        _bypassed = old._bypassed;
    }

    internal void Process(ref double left, ref double right, PreparedReverb p)
    {
        if (!p.Enabled && _bypassed) return;
        _bypassed = false;
        _bank.Settings = p;
        _mix += ((p.Enabled ? p.Mix : 0) - _mix) * _smoothing;
        if (!p.Enabled && _mix < 1e-8)
        {
            Reset(p);
            return;
        }
        _tail = Math.Max(_tail, p.TailFrames);
        if (left != 0 || right != 0) _remaining = _tail;
        if (_remaining == 0) return;
        _bank.Process(left, right, out double wetL, out double wetR);
        if (_transition > 0 && _fading is { } fading)
        {
            fading.Process(left, right, out double oldL, out double oldR);
            double blend = 1 - (double)_transition / _transitionFrames;
            wetL = oldL * (1 - blend) + wetL * blend;
            wetR = oldR * (1 - blend) + wetR * blend;
            if (--_transition == 0) fading.Reset();
        }
        double fade = Math.Min(1, (double)_remaining / Math.Max(1, _rate / 200));
        left = left * (1 - _mix) + wetL * _mix * fade;
        right = right * (1 - _mix) + wetR * _mix * fade;
        if (--_remaining == 0)
        {
            _bank.Reset();
            _fading?.Reset();
        }
    }
}
