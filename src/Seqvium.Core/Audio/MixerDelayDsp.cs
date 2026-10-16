// SPDX-License-Identifier: Apache-2.0

namespace Seqvium.Core;

internal readonly record struct PreparedDelay(bool Enabled, int TimeFrames, double Feedback, double Mix, long TailFrames);

/// <summary>Consumer-owned stereo feedback line. Storage is reserved outside rendering, including bypassed strips.
/// Generation tags make Stop/Seek O(1); continuation transfers ownership without copying PCM in the callback.</summary>
internal sealed class MixerDelayDsp
{
    internal const int MaximumBytes = 64 * 1024 * 1024;
    internal static int StorageBytes(int rate) => checked((rate + 1) * (2 * sizeof(float) + sizeof(long)));
    // Include repeats through -80 dB relative decay, one extra period and transition settling.
    internal static long TailFrames(MixerDelay delay, int rate) => delay.Enabled
        ? checked(Frames(delay, rate) * (delay.Feedback == 0 ? 1L :
            (long)Math.Ceiling(Math.Log(.0001) / Math.Log((double)delay.Feedback)) + 1) + rate / 20) : 0;
    internal static PreparedDelay Prepare(MixerDelay delay, int rate) =>
        new(delay.Enabled, Frames(delay, rate), (double)delay.Feedback, (double)delay.Mix, TailFrames(delay, rate));

    private static int Frames(MixerDelay delay, int rate) =>
        (int)decimal.Round(delay.TimeMs * rate / 1000, 0, MidpointRounding.AwayFromZero);

    private readonly float[] _left, _right;
    private readonly long[] _tags;
    private readonly int _rate, _transitionFrames;
    private readonly double _smoothing;
    private long _generation = 1, _remaining, _tail;
    private int _write, _time, _nextTime, _transition;
    private double _feedback, _mix;
    private bool _bypassed;

    internal MixerDelayDsp(PreparedDelay settings, int rate)
    {
        _rate = rate;
        _left = new float[rate + 1];
        _right = new float[rate + 1];
        _tags = new long[rate + 1];
        _transitionFrames = rate / 50;
        _smoothing = 1 - Math.Exp(-1 / (.005 * rate));
        Reset(settings);
    }

    internal void Reset(PreparedDelay settings)
    {
        _generation++;
        _remaining = 0;
        _tail = settings.TailFrames;
        _time = _nextTime = settings.TimeFrames;
        _transition = 0;
        _feedback = settings.Feedback;
        _mix = settings.Enabled ? settings.Mix : 0;
        _bypassed = !settings.Enabled;
    }

    internal void Process(ref double left, ref double right, PreparedDelay settings)
    {
        if (!settings.Enabled && _bypassed) return;
        _bypassed = false;
        _feedback += (settings.Feedback - _feedback) * _smoothing;
        _mix += ((settings.Enabled ? settings.Mix : 0) - _mix) * _smoothing;
        if (!settings.Enabled && _mix < 1e-8)
        {
            Reset(settings);
            return;
        }

        int targetTime = settings.TimeFrames;
        if (_transition == 0 && targetTime != _time)
        {
            _nextTime = targetTime;
            _transition = _transitionFrames;
        }
        _tail = Math.Max(_tail, settings.TailFrames);
        if (left != 0 || right != 0) _remaining = _tail;
        if (_remaining == 0) return;

        int old = (_write + _left.Length - _time) % _left.Length;
        int next = (_write + _left.Length - _nextTime) % _left.Length;
        double blend = _transition == 0 ? 0 : 1 - (double)_transition / _transitionFrames;
        double wetL = (_tags[old] == _generation ? _left[old] : 0) * (1 - blend) +
                      (_tags[next] == _generation ? _left[next] : 0) * blend;
        double wetR = (_tags[old] == _generation ? _right[old] : 0) * (1 - blend) +
                      (_tags[next] == _generation ? _right[next] : 0) * blend;
        double fade = Math.Min(1, (double)_remaining / (_rate / 200));
        double storedL = left + wetL * _feedback, storedR = right + wetR * _feedback;
        if (!double.IsFinite(storedL) || !double.IsFinite(storedR) ||
            Math.Abs(storedL) > float.MaxValue || Math.Abs(storedR) > float.MaxValue)
        {
            left = right = double.NaN;
            Reset(settings);
            return;
        }
        _left[_write] = (float)(storedL * fade);
        _right[_write] = (float)(storedR * fade);
        _tags[_write] = _generation;
        left = left * (1 - _mix) + wetL * _mix * fade;
        right = right * (1 - _mix) + wetR * _mix * fade;
        if (++_write == _left.Length) _write = 0;
        if (_transition > 0 && --_transition == 0) _time = _nextTime;
        if (--_remaining == 0) _generation++;
    }
}
