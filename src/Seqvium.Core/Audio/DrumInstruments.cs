// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

/// <summary>The finite first-party drum family. Descriptors own sound parameters, not rhythm or plugin hosting.</summary>
public static class DrumInstruments
{
    public sealed record Preset(string Name, ImmutableDictionary<string, decimal> Parameters);

    public sealed record Instrument(
        string Key,
        string Algorithm,
        ImmutableArray<DrumVoice.Parameter> Parameters,
        ImmutableArray<Preset> Presets,
        bool HasPitchEnvelope = false,
        bool IsHat = false)
    {
        public ImmutableDictionary<string, decimal> Defaults => Presets[0].Parameters;
    }

    private static DrumVoice.Parameter P(string key, decimal min, decimal max, decimal value, string unit = "%") =>
        new(key, min, max, value, unit);

    private static Instrument Create(string key, ImmutableArray<DrumVoice.Parameter> parameters,
        string second, (string, decimal)[] changes, string third, (string, decimal)[] more,
        bool pitched = false, bool hat = false)
    {
        var defaults = parameters.ToImmutableDictionary(p => p.Key, p => p.Default)
            .Add("rootPitch", DrumVoice.RootPitch);

        ImmutableDictionary<string, decimal> Change((string Key, decimal Value)[] edits) =>
            edits.Aggregate(defaults, (values, edit) => values.SetItem(edit.Key, edit.Value));

        return new(key, key == "Kick" ? DrumVoice.Algorithm : "core.drum-voice." + key.ToLowerInvariant() + "-v1",
            parameters, [new("Classic", defaults), new(second, Change(changes)), new(third, Change(more))], pitched,
            hat);
    }

    public static ImmutableArray<Instrument> All { get; } =
    [
        Create("Kick", DrumVoice.Parameters, "Deep", [("tune", -8), ("decay", 850), ("tone", .12m)],
            "Punch", [("decay", 260), ("tone", .55m), ("noise", .09m), ("pitchAmount", 38)], true),
        Create("Snare", [
                P("tune", -24, 24, 0, "st"), P("decay", 60, 1200, 240, "ms"),
                P("body", 0, 1, .55m), P("snappy", 0, 1, .72m), P("brightness", 0, 1, .55m),
                P("attack", .2m, 30, .5m, "ms"), P("amplitudeCurve", .25m, 5, 2.2m, "")
            ],
            "Tight", [("decay", 130), ("body", .7m), ("snappy", .48m)],
            "Dust", [("decay", 380), ("brightness", .22m), ("body", .3m), ("snappy", .85m)]),
        Create("ClosedHat", [
                P("tune", -24, 24, 0, "st"), P("decay", 20, 400, 85, "ms"),
                P("metal", 0, 1, .7m), P("brightness", 0, 1, .7m), P("choke", 0, 1, 1, ""),
                P("attack", .2m, 10, .3m, "ms"), P("amplitudeCurve", .25m, 5, 2.4m, "")
            ],
            "Tick", [("decay", 38), ("metal", .9m), ("brightness", .9m)],
            "Soft", [("decay", 130), ("metal", .35m), ("brightness", .3m)], hat: true),
        Create("OpenHat", [
                P("tune", -24, 24, 0, "st"), P("decay", 100, 2400, 620, "ms"),
                P("metal", 0, 1, .75m), P("brightness", 0, 1, .65m), P("choke", 0, 1, 1, ""),
                P("attack", .2m, 30, .8m, "ms"), P("amplitudeCurve", .25m, 5, 1.7m, "")
            ],
            "Shimmer", [("decay", 1200), ("brightness", .9m), ("metal", .9m)],
            "Short", [("decay", 330), ("brightness", .4m), ("metal", .5m)], hat: true),
        Create("Clap", [
                P("tune", -24, 24, 0, "st"), P("decay", 80, 1200, 280, "ms"),
                P("spread", 5, 35, 13, "ms"), P("brightness", 0, 1, .5m), P("tail", 0, 1, .45m),
                P("attack", .2m, 10, .4m, "ms"), P("amplitudeCurve", .25m, 5, 2, "")
            ],
            "Wide", [("decay", 480), ("spread", 23), ("tail", .65m)],
            "Dry", [("decay", 150), ("spread", 8), ("tail", .2m)]),
        Create("Tom", [
                P("tune", -24, 24, 0, "st"), P("decay", 80, 2000, 430, "ms"),
                P("resonance", 0, 1, .5m), P("noise", 0, 1, .08m),
                P("pitchAmount", 0, 36, 9, "st"), P("pitchDecay", 5, 500, 85, "ms"),
                P("pitchCurve", .25m, 5, 2, ""), P("attack", .2m, 30, .6m, "ms"),
                P("amplitudeCurve", .25m, 5, 2, "")
            ],
            "Low", [("tune", -7), ("decay", 650), ("resonance", .65m)],
            "High", [("tune", 7), ("decay", 230), ("resonance", .35m)], true),
        Create("Rim", [
                P("tune", -24, 24, 0, "st"), P("decay", 20, 600, 100, "ms"),
                P("wood", 0, 1, .6m), P("click", 0, 1, .25m), P("brightness", 0, 1, .55m),
                P("attack", .2m, 10, .25m, "ms"), P("amplitudeCurve", .25m, 5, 2.7m, "")
            ],
            "Clave", [("wood", .95m), ("click", .05m), ("decay", 160), ("brightness", .3m)],
            "Edge", [("wood", .15m), ("click", .6m), ("brightness", .85m), ("decay", 65)])
    ];

    public static Instrument? Find(string algorithm) => All.FirstOrDefault(i => i.Algorithm == algorithm);

    public static double Frequency(string algorithm, ImmutableDictionary<string, decimal> values,
        decimal pitch = DrumVoice.RootPitch)
    {
        double fundamental = Find(algorithm)?.Key switch
        {
            "Snare" => 185, "Tom" => 130, "ClosedHat" or "OpenHat" => 4211, "Clap" => 900, "Rim" => 680, _ => 0
        };
        if (Find(algorithm)?.Key == "Rim")
            fundamental = (double)values["wood"] * 680 + (1 - (double)values["wood"]) * 1600;
        return fundamental == 0
            ? DrumVoice.Frequency(values, pitch)
            : fundamental * Math.Pow(2, (double)(pitch - DrumVoice.RootPitch + values["tune"]) / 12);
    }

    public static bool Supports(SoundDefinition sound)
    {
        if (sound.Algorithm == DrumVoice.Algorithm) return DrumVoice.SupportsKick(sound);
        return Find(sound.Algorithm) is { } instrument && sound.Extension is null && sound.AdditionalData.IsEmpty &&
               sound.ResourceIds.IsEmpty && sound.Parameters.Count == instrument.Defaults.Count &&
               sound.Parameters.TryGetValue("rootPitch", out var root) && root == DrumVoice.RootPitch &&
               instrument.Parameters.All(p => sound.Parameters.TryGetValue(p.Key, out var value) &&
                                              value >= p.Minimum && value <= p.Maximum) &&
               (!instrument.IsHat || sound.Parameters["choke"] is 0 or 1);
    }

    public static void Validate(string algorithm, ImmutableDictionary<string, decimal> values)
    {
        if (!Supports(new(default, "Drum", algorithm, values, [], null)))
            throw new NotSupportedException("Unsupported built-in drum parameters.");
    }

    internal static int ChokeGroup(SoundDefinition sound) => Find(sound.Algorithm) is { IsHat: true } &&
                                                             sound.Parameters["choke"] == 1
        ? 1
        : 0;

    /// <summary>Two-substep deterministic synthesis, generated off the realtime thread at host rate.
    /// Modes fade at Nyquist; noise filters and amplitude attack suppress DC and discontinuities.</summary>
    internal static DecodedPcm Render(SoundDefinition sound, int rate, decimal pitch, CancellationToken cancellation)
    {
        Validate(sound.Algorithm, sound.Parameters);
        if (rate is not (44100 or 48000) || pitch is < 0 or > 127)
            throw new NotSupportedException("Drums support 44.1/48 kHz and MIDI pitches 0–127.");
        var instrument = Find(sound.Algorithm)!;
        var p = sound.Parameters;
        double V(string key) => (double)p[key];
        double tune = Math.Pow(2, (double)(pitch - DrumVoice.RootPitch + p["tune"]) / 12);
        double decay = V("decay"), brightness = p.ContainsKey("brightness") ? V("brightness") : .5;
        int frames = checked((int)decimal.Ceiling(p["decay"] * rate / 1000));
        var samples = new float[frames];
        var phases = new double[6];
        double[] frequencies = [4211, 5873, 6839, 7919, 9733, 11213];
        double low = 0, highLow = 0;
        double upper =
            1 - Math.Exp(-2 * Math.PI * Math.Min(rate * .4, (1800 + brightness * 11000) * tune) / (rate * 2));
        double lower = 1 - Math.Exp(-2 * Math.PI * Math.Min(rate * .3,
            (instrument.IsHat ? 2200 + brightness * 4000 : 500 + brightness * 800) * tune) / (rate * 2));
        uint random = 0x6D2B79F5;

        double Mode(int index, double hz, double ms, double damping = 1)
        {
            double audible = Math.Clamp((rate * .49 - hz) / (rate * .04), 0, 1);
            phases[index] = (phases[index] + 2 * Math.PI * Math.Min(hz, rate * .49) / (rate * 2)) % (2 * Math.PI);
            return Math.Sin(phases[index]) * audible * Math.Exp(-ms * damping / decay);
        }

        for (int frame = 0; frame < frames; frame++)
        {
            if ((frame & 2047) == 0) cancellation.ThrowIfCancellationRequested();
            double sum = 0;
            for (int sub = 0; sub < 2; sub++)
            {
                double ms = (frame + sub * .5) * 1000 / rate;
                random ^= random << 13;
                random ^= random >> 17;
                random ^= random << 5;
                double white = random / (double)uint.MaxValue * 2 - 1;
                low += upper * (white - low);
                highLow += lower * (low - highLow);
                double band = low - highLow;
                double env = DrumVoice.AmplitudeEnvelope(p, ms);
                double signal;
                switch (instrument.Key)
                {
                    case "Snare":
                        double drop = 1 + .65 * Math.Exp(-ms / 13);
                        double body = .72 * Mode(0, 185 * tune * drop, ms, 5) +
                                      .28 * Mode(1, 330 * tune * drop, ms, 7);
                        signal = (.55 * V("body") * body + 1.3 * V("snappy") * band) * env;
                        break;
                    case "ClosedHat":
                    case "OpenHat":
                        // Six inharmonic upper modes give the metal layer its own identity without aliasing square waves.
                        double metal = 0;
                        for (int i = 0; i < 6; i++) metal += Mode(i, frequencies[i] * tune, ms, .3 + i * .12);
                        signal = (metal * .13 * V("metal") + band * 1.2 * (1 - V("metal") * .55)) * env * .65;
                        break;
                    case "Clap":
                        double spread = V("spread"), bursts = 0;
                        for (int i = 0; i < 3; i++)
                        {
                            double age = ms - i * spread;
                            if (age >= 0)
                                bursts += Math.Exp(-age / (spread * .27)) *
                                          (.5 - .5 * Math.Cos(Math.PI * Math.Clamp(age / V("attack"), 0, 1)));
                        }

                        double tailAge = Math.Max(0, ms - spread * 2);
                        double tail = ms >= spread * 2 ? V("tail") * Math.Exp(-tailAge / (decay * .24)) : 0;
                        signal = 1.6 * band * (bursts + tail) *
                                 Math.Pow(Math.Clamp(1 - ms / decay, 0, 1), V("amplitudeCurve"));
                        break;
                    case "Tom":
                        double hz = 130 * tune * Math.Pow(2, DrumVoice.PitchEnvelope(p, ms) / 12);
                        double modes = Mode(0, hz, ms) + V("resonance") *
                            (.42 * Mode(1, hz * 1.59, ms, 2) + .2 * Mode(2, hz * 2.13, ms, 3));
                        signal = .75 * (modes / (1 + V("resonance") * .62) * env +
                                        V("noise") * band * Math.Exp(-ms / 12) * env);
                        break;
                    default:
                        double wood = V("wood");
                        double ring = .62 * Mode(0, (wood * 680 + (1 - wood) * 1600) * tune, ms, 2) +
                                      .26 * Mode(1, 2370 * tune, ms, 4) + .12 * Mode(2, 3580 * tune, ms, 6);
                        signal = .65 * (ring + V("click") * band * 2 * Math.Exp(-ms / 5)) * env;
                        break;
                }

                sum += signal;
            }

            // Smooth saturation is part of this family's sound, not a hidden master limiter.
            samples[frame] = frame == 0 || frame == frames - 1 ? 0 : (float)(.85 * Math.Tanh(sum * .5));
        }

        return new(rate, 1, samples);
    }
}
