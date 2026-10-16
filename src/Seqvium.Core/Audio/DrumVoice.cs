// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

/// <summary>Versioned built-in percussion. Canonical parameters are independent of temporary rendered PCM.</summary>
public static class DrumVoice
{
    public const string Algorithm = "core.drum-voice.kick-v1";
    public const decimal RootPitch = 36;

    public sealed record Parameter(string Key, decimal Minimum, decimal Maximum, decimal Default, string Unit);

    public static ImmutableArray<Parameter> Parameters { get; } =
    [
        new("tune", -24, 24, -3, "st"),
        new("decay", 60, 2000, 480, "ms"),
        new("tone", 0, 1, .25m, "%"),
        new("noise", 0, 1, .035m, "%"),
        new("pitchAmount", 0, 60, 30, "st"),
        new("pitchDecay", 5, 500, 65, "ms"),
        new("pitchCurve", .25m, 5, 2.4m, ""),
        new("attack", .2m, 30, .8m, "ms"),
        new("amplitudeCurve", .25m, 5, 2.6m, "")
    ];

    public static ImmutableDictionary<string, decimal> Defaults { get; } = Parameters
        .ToImmutableDictionary(p => p.Key, p => p.Default).Add("rootPitch", RootPitch);

    public static bool Supports(SoundDefinition sound) => DrumInstruments.Supports(sound);

    internal static bool SupportsKick(SoundDefinition sound) => sound.Algorithm == Algorithm &&
                                                                sound.Extension is null &&
                                                                sound.AdditionalData.IsEmpty &&
                                                                sound.ResourceIds.IsEmpty &&
                                                                sound.Parameters.Count == Defaults.Count &&
                                                                sound.Parameters.TryGetValue("rootPitch",
                                                                    out var root) &&
                                                                root == RootPitch && Parameters.All(p =>
                                                                    sound.Parameters.TryGetValue(p.Key,
                                                                        out var value) &&
                                                                    value >= p.Minimum && value <= p.Maximum);

    public static void Validate(ImmutableDictionary<string, decimal> parameters)
    {
        if (!Supports(new(default, "Drum Voice", Algorithm, parameters, [], null)))
            throw new NotSupportedException("Unsupported Drum Voice state or parameter range.");
    }

    public static DecodedPcm Render(SoundDefinition sound, int sampleRate, decimal pitch = RootPitch,
        CancellationToken cancellation = default)
    {
        if (!Supports(sound)) throw new NotSupportedException("Unsupported Drum Voice state.");
        return sound.Algorithm == Algorithm
            ? Render(sound.Parameters, sampleRate, pitch, cancellation)
            : DrumInstruments.Render(sound, sampleRate, pitch, cancellation);
    }

    public static double Frequency(ImmutableDictionary<string, decimal> parameters, decimal pitch = RootPitch) =>
        440 * Math.Pow(2, (double)(pitch + parameters["tune"] - 69) / 12);

    public static double PitchEnvelope(ImmutableDictionary<string, decimal> parameters, double milliseconds) =>
        (double)parameters["pitchAmount"] * Math.Pow(Math.Clamp(1 - milliseconds /
            (double)parameters["pitchDecay"], 0, 1), (double)parameters["pitchCurve"]);

    public static double AmplitudeEnvelope(ImmutableDictionary<string, decimal> parameters, double milliseconds)
    {
        double attack = Math.Clamp(milliseconds / (double)parameters["attack"], 0, 1);
        return (.5 - .5 * Math.Cos(Math.PI * attack)) * Math.Pow(Math.Clamp(1 - milliseconds /
            (double)parameters["decay"], 0, 1), (double)parameters["amplitudeCurve"]);
    }

    /// <summary>Bounded off-thread synthesis at host rate and exact note pitch; no file or persistent sample is created.
    /// Each hit uses the same deterministic noise seed. Velocity is applied once by the shared voice scheduler.</summary>
    public static DecodedPcm Render(ImmutableDictionary<string, decimal> parameters, int sampleRate,
        decimal pitch = RootPitch, CancellationToken cancellation = default)
    {
        Validate(parameters);
        if (sampleRate is not (44100 or 48000) || pitch is < 0 or > 127)
            throw new NotSupportedException("Drum Voice supports 44.1/48 kHz and MIDI pitches 0–127.");
        int frames = checked((int)decimal.Ceiling(parameters["decay"] * sampleRate / 1000));
        var samples = new float[frames];
        double phase = 0, filteredNoise = 0, baseHz = Frequency(parameters, pitch);
        double tone = (double)parameters["tone"], noise = (double)parameters["noise"];
        double coefficient = 1 - Math.Exp(-2 * Math.PI * (600 + tone * 9400) / (sampleRate * 2));
        double noiseLength = Math.Min(140, (double)parameters["decay"] * .25);
        uint random = 0x6D2B79F5;
        for (int frame = 0; frame < frames; frame++)
        {
            if ((frame & 2047) == 0) cancellation.ThrowIfCancellationRequested();
            double sample = 0;
            // Two substeps smooth the pitched attack and noise filter. Harmonics approaching
            // the output Nyquist limit fade out; high MIDI pitches never fold into bass aliases.
            for (int sub = 0; sub < 2; sub++)
            {
                double ms = (frame + sub * .5) * 1000 / sampleRate;
                double hz = baseHz * Math.Pow(2, PitchEnvelope(parameters, ms) / 12);
                double audible = Math.Clamp((sampleRate * .49 - hz) / (sampleRate * .04), 0, 1);
                hz = Math.Min(hz, sampleRate * .49);
                double body = Math.Sin(phase) + tone * .35 * Math.Sin(phase * 2) *
                              Math.Clamp((sampleRate * .49 - hz * 2) / (sampleRate * .04), 0, 1) +
                              tone * .18 * Math.Sin(phase * 3) *
                              Math.Clamp((sampleRate * .49 - hz * 3) / (sampleRate * .04), 0, 1);
                phase = (phase + 2 * Math.PI * hz / (sampleRate * 2)) % (2 * Math.PI);
                random ^= random << 13;
                random ^= random >> 17;
                random ^= random << 5;
                double white = random / (double)uint.MaxValue * 2 - 1;
                filteredNoise += coefficient * (white - filteredNoise);
                double noiseEnvelope = Math.Pow(Math.Clamp(1 - ms / noiseLength, 0, 1), 2);
                double envelope = AmplitudeEnvelope(parameters, ms);
                sample += .8 * ((1 - noise * .45) * body * audible / (1 + tone * .53) * envelope +
                                noise * filteredNoise * noiseEnvelope *
                                (.5 - .5 * Math.Cos(Math.PI * Math.Clamp(ms / .3, 0, 1))));
            }

            samples[frame] = frame == 0 || frame == frames - 1 ? 0 : (float)(sample * .5);
        }

        return new(sampleRate, 1, samples);
    }
}

/// <summary>The two implemented first-party sound capabilities, not an extension registry.</summary>
public static class BuiltInSound
{
    public static bool Supports(SoundDefinition sound) => SynthVoice.Supports(sound) || DrumVoice.Supports(sound) || SamplerPlayback.Supports(sound);
}
