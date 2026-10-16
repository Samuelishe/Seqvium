// SPDX-License-Identifier: Apache-2.0

namespace Seqvium.Core;

/// <summary>Immutable host-rate source preview, prepared off the consumer. No document or media ownership.</summary>
public sealed class PreparedDrumAudition
{
    internal readonly float[] Samples;
    public int SampleRate { get; }
    public int Frames => Samples.Length;

    private PreparedDrumAudition(int rate, float[] samples)
    {
        SampleRate = rate;
        Samples = samples;
    }

    public static PreparedDrumAudition Prepare(SoundDefinition sound, int rate, decimal pitch,
        CancellationToken cancellation = default)
    {
        using var pcm = DrumVoice.Render(sound, rate, pitch, cancellation);
        using var lease = pcm.Lease();
        // The immutable array is strongly owned by this source and each borrowing fixed voice.
        // No lease disposal or PCM reclamation runs on the audio callback.
        return new(rate, lease.Samples);
    }
}

/// <summary>Eight fixed host-rate drum cursors in the existing consumer, after musical Master.
/// One serialized producer, one consumer. The bounded ring never retries or schedules a late hit.</summary>
internal sealed class DrumAudition
{
    internal const int Capacity = 8;

    private readonly record struct Hit(PreparedDrumAudition Source, float Velocity, long Stop);

    private struct Voice
    {
        internal PreparedDrumAudition? Source;
        internal int Position;
        internal float Velocity;
        internal long Order;
    }

    private readonly Hit[] _commands = new Hit[Capacity];
    private readonly Voice[] _voices = new Voice[Capacity];
    private readonly AuditionPeakProtection _protection;
    private long _write, _read, _stop, _seenStop, _hits, _overflow, _stolen;
    private int _active;
    internal int ActiveVoices => Volatile.Read(ref _active);
    internal long Hits => Volatile.Read(ref _hits);
    internal long Overflow => Volatile.Read(ref _overflow);
    internal long Stolen => Volatile.Read(ref _stolen);
    internal float MinimumGain => _protection.MinimumGain;

    internal DrumAudition(int rate) => _protection = new(rate);

    internal bool Trigger(PreparedDrumAudition source, decimal velocity)
    {
        if (velocity is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(velocity));
        long write = _write;
        if (write - Volatile.Read(ref _read) == Capacity)
        {
            Interlocked.Increment(ref _overflow);
            return false;
        }

        _commands[write % Capacity] = new(source, (float)velocity, Volatile.Read(ref _stop));
        Volatile.Write(ref _write, write + 1);
        return true;
    }

    internal void Stop() => Interlocked.Increment(ref _stop);

    internal void Process(Span<float> output, int channels)
    {
        long stop = Volatile.Read(ref _stop);
        if (stop != _seenStop)
        {
            Array.Clear(_voices);
            _protection.Reset();
            _seenStop = stop;
        }

        long write = Volatile.Read(ref _write);
        long read = _read;
        // At most eight commands at this boundary, regardless of concurrent producer progress.
        while (read < write)
        {
            var hit = _commands[read % Capacity];
            _commands[read % Capacity] = default;
            read++;
            if (hit.Stop != stop) continue;
            int slot = -1;
            for (int i = 0; i < Capacity; i++)
                if (_voices[i].Source is null)
                {
                    slot = i;
                    break;
                }

            if (slot < 0)
            {
                slot = 0;
                for (int i = 1; i < Capacity; i++)
                    if (_voices[i].Order < _voices[slot].Order)
                        slot = i;
                _stolen++;
            }

            _voices[slot] = new() { Source = hit.Source, Velocity = hit.Velocity, Order = ++_hits };
        }

        Volatile.Write(ref _read, read);

        bool sounding = false;
        for (int i = 0; i < Capacity; i++) sounding |= _voices[i].Source is not null;
        if (!sounding)
        {
            Volatile.Write(ref _active, 0);
            return;
        }

        int active = 0;
        for (int frame = 0; frame < output.Length / channels; frame++)
        {
            float preview = 0;
            for (int i = 0; i < Capacity; i++)
            {
                ref var voice = ref _voices[i];
                if (voice.Source is not { } source) continue;
                preview += source.Samples[voice.Position++] * voice.Velocity;
                if (voice.Position == source.Frames) voice.Source = null;
            }

            int offset = frame * channels;
            float music = Math.Abs(output[offset]);
            if (channels == 2) music = Math.Max(music, Math.Abs(output[offset + 1]));
            // Reserve only the available sample headroom. Never duck/limit the musical graph.
            float gain = _protection.Gain(Math.Abs(preview), music);
            for (int channel = 0; channel < channels; channel++) output[offset + channel] += preview * gain;
        }

        for (int i = 0; i < Capacity; i++)
            if (_voices[i].Source is not null)
                active++;
        Volatile.Write(ref _active, active);
        if (active == 0) _protection.Reset();
    }

    // Called only by a joined/terminal consumer, never concurrently with Process.
    internal void Clear()
    {
        Array.Clear(_commands);
        Array.Clear(_voices);
        _read = _write;
        Volatile.Write(ref _active, 0);
    }
}

/// <summary>Linked sample-peak preview protection: instant attenuation, 50-ms recovery, no makeup.</summary>
internal sealed class AuditionPeakProtection(int rate)
{
    internal const float Ceiling = .89125094f; // -1 dBFS; sample peaks, not true peaks or loudness.
    private readonly float _recovery = (float)(1 - Math.Exp(-1d / (.05 * rate)));
    private float _gain = 1, _minimum = 1;
    internal float MinimumGain => Volatile.Read(ref _minimum);

    internal float Gain(float preview, float music = 0)
    {
        float available = Math.Max(0, Ceiling - music);
        float target = preview > available ? available / preview : 1;
        _gain = target < _gain ? target : Math.Min(target, _gain + (1 - _gain) * _recovery);
        if (_gain < _minimum) Volatile.Write(ref _minimum, _gain);
        return _gain;
    }

    internal void Reset() => _gain = 1;
}
