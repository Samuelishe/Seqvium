// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using Seqvium.Core;

namespace Seqvium.Desktop.Extensions;

/// <summary>Optional first-party offline percussion recipes. No dependency on Drum Voice or audio devices.</summary>
internal sealed class PercussionGenerator : IExploringSampleGenerator
{
    internal const string Package = "seqvium.percussion-lab", Capability = "seqvium.percussion-lab.percussion", Version = "1.0.0";
    internal static ExtensionHost CreateHost() => new(new Dictionary<ExtensionProviderKey, Func<CancellationToken, Task<ISampleGenerator>>>
    {
        [new(Package, Version, Capability)] = token =>
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult<ISampleGenerator>(new PercussionGenerator());
        }
    });
    public ImmutableArray<SampleParameter> Parameters => [];
    public ImmutableArray<SampleFamily> Families { get; } =
    [
        new("Kick", [new("pitch", 35, 85, 52, "Hz"), new("decay", 100, 1000, 450, "ms"),
            new("sweep", 0, 1, .5m, ""), new("attack", 0, 1, .35m, ""),
            new("color", 0, 1, .25m, ""), new("drive", 0, 1, .2m, "")]),
        new("Clap", [new("color", 700, 3000, 1700, "Hz"), new("decay", 80, 650, 260, "ms"),
            new("spread", 8, 28, 15, "ms"), new("attack", 0, 1, .6m, ""), new("body", 0, 1, .4m, "")])
    ];
    private bool _disposed;

    public Task<SampleCandidate> GenerateAsync(SampleGenerationRequest request, CancellationToken cancellation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var explore = request.Exploration ?? throw new ArgumentException("Lab.ReferenceRequired");
        explore.Validate(Families);
        var family = Families.Single(f => f.Id == explore.Family);
        var random = new Random(explore.Seed);
        var values = ImmutableDictionary.CreateBuilder<string, decimal>(StringComparer.Ordinal);
        // Correlated styles keep broad random exploration in a useful musical region.
        double style = random.NextDouble();
        foreach (var control in family.Controls)
        {
            double normalized = random.NextDouble();
            if (explore.Kind == SampleExplorationKind.Similar)
            {
                double basis = (double)((explore.Reference!.Values[control.Id] - control.Minimum) / (control.Maximum - control.Minimum));
                normalized = Math.Clamp(basis + (random.NextDouble() + random.NextDouble() - 1) * .16, 0, 1);
            }
            else if (family.Id == "Kick")
                normalized = control.Id switch
                {
                    "decay" => .08 + .82 * style * style,
                    "pitch" => .2 + .55 * (1 - style) + .2 * (normalized - .5),
                    "drive" => normalized * (.25 + .7 * style),
                    "sweep" => .15 + .75 * normalized,
                    _ => normalized
                };
            else if (control.Id == "decay") normalized = .15 + .7 * style;
            decimal value = control.Minimum + (control.Maximum - control.Minimum) * (decimal)normalized;
            values[control.Id] = explore.Locks.TryGetValue(control.Id, out var held) ? held : decimal.Round(value, control.Maximum <= 1 ? 2 : 0);
        }
        var recipe = new SampleRecipe(family.Id, explore.Seed, values.ToImmutable(),
            explore.Kind == SampleExplorationKind.Similar ? explore.ReferenceId : null, explore.Locks);
        double Value(string key) => (double)recipe.Values[key];
        int rate = request.Context.SampleRate;
        double duration = (Value("decay") + (family.Id == "Clap" ? Value("spread") * 2 : 0) + 12) / 1000;
        int frames = (int)Math.Ceiling(duration * rate);
        if (rate is < 8000 or > 192000 || frames < 1 || frames > request.Context.MaximumFrames || frames > rate * 2)
            throw new ArgumentException("Extension.InvalidResult");
        var samples = new double[frames];
        double phase = 0, previous = 0, highpass = 0, low = 0, bandLow = 0, peak = 0;
        double decay = Value("decay") / 1000;
        for (int i = 0; i < frames; i++)
        {
            if ((i & 1023) == 0) cancellation.ThrowIfCancellationRequested();
            double t = (double)i / rate, noise = random.NextDouble() * 2 - 1, sample;
            if (family.Id == "Kick")
            {
                double sweep = Value("sweep"), color = Value("color"), attack = Value("attack");
                double frequency = Value("pitch") * (1 + (2 + 6 * sweep) * Math.Exp(-t / (.008 + .045 * sweep)));
                phase += 2 * Math.PI * frequency / rate;
                double body = (Math.Sin(phase) + .25 * color * Math.Sin(2 * phase) + .12 * color * Math.Sin(3 * phase)) *
                    Math.Exp(-6.907755 * t / decay) * Math.Min(1, t / .0015);
                low += (noise - low) * (1 - Math.Exp(-2 * Math.PI * 1600 / rate));
                double click = (noise - low) * attack * .5 * Math.Exp(-t / (.0018 + .005 * attack));
                sample = Math.Tanh((body + click) * (1 + 4 * Value("drive")));
            }
            else
            {
                double color = Value("color");
                low += (noise - low) * (1 - Math.Exp(-2 * Math.PI * Math.Min(rate * .42, color * 1.8) / rate));
                bandLow += (low - bandLow) * (1 - Math.Exp(-2 * Math.PI * color * .45 / rate));
                double spread = Value("spread") / 1000, burst = 0;
                for (int pulse = 0; pulse < 3; pulse++)
                {
                    double age = t - spread * pulse;
                    if (age >= 0) burst += Math.Exp(-age / .0035) * Math.Min(1, age / .0007);
                }
                double tailAge = Math.Max(0, t - 2 * spread);
                double tail = t < 2 * spread ? 0 : Math.Exp(-6.907755 * tailAge / decay) * Math.Min(1, tailAge / .002);
                sample = (low - bandLow) * (burst * (.7 + Value("attack")) + tail * (.4 + .6 * Value("body")));
                sample += .08 * Value("body") * Math.Sin(2 * Math.PI * color * .27 * t) * tail;
            }
            // Remove DC, soften the terminal edge and peak-bound the finite result.
            highpass = sample - previous + Math.Exp(-2 * Math.PI * 15 / rate) * highpass;
            previous = sample;
            samples[i] = highpass * Math.Min(1, (double)(frames - 1 - i) / Math.Max(1, rate / 200));
            peak = Math.Max(peak, Math.Abs(samples[i]));
        }
        using var output = new MemoryStream(44 + frames * 2);
        using (var writer = new BinaryWriter(output, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write("RIFF"u8); writer.Write(36 + frames * 2); writer.Write("WAVEfmt "u8);
            writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(rate);
            writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
            writer.Write("data"u8); writer.Write(frames * 2);
            foreach (double sample in samples) writer.Write((short)Math.Round(sample / Math.Max(peak, .000001) * .85 * short.MaxValue));
        }
        cancellation.ThrowIfCancellationRequested();
        return Task.FromResult(new SampleCandidate(family.Id + " " + ((uint)explore.Seed).ToString("X8"), output.ToArray()) { Recipe = recipe });
    }
    public ValueTask DisposeAsync() { _disposed = true; return ValueTask.CompletedTask; }
}
