// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

public sealed record MixerEq : CanonicalData
{
    public bool Enabled { get; init; }
    public decimal HighPassHz { get; init; }
    public decimal LowDb { get; init; }
    public decimal MidHz { get; init; } = 1000;
    public decimal MidDb { get; init; }
    public decimal MidQ { get; init; } = 1;
    public decimal HighDb { get; init; }
}

public sealed record MixerCompressor : CanonicalData
{
    public bool Enabled { get; init; }
    public decimal ThresholdDb { get; init; } = -18;
    public decimal Ratio { get; init; } = 3;
    public decimal AttackMs { get; init; } = 10;
    public decimal ReleaseMs { get; init; } = 120;
    public decimal KneeDb { get; init; } = 6;
    public decimal MakeupDb { get; init; }
}

public sealed record MixerStripSettings : CanonicalData
{
    public decimal TrimDb { get; init; }

    // -90 is an exact silence detent, not a tiny nonzero gain.
    public decimal LevelDb { get; init; }
    public decimal Pan { get; init; }
    public bool Muted { get; init; }
    public bool Solo { get; init; }
    public MixerEq Eq { get; init; } = new();
    public MixerCompressor Compressor { get; init; } = new();
}

public sealed record MixerSend(Id<MixerChannel> Target, decimal LevelDb, bool PreFader) : CanonicalData;

public sealed record MixerChannel(
    Id<MixerChannel> Id,
    string Name,
    int ColorIndex,
    ImmutableArray<Id<SoundDefinition>> SoundIds,
    Id<MixerChannel>? Output,
    ImmutableArray<MixerSend> Sends,
    MixerStripSettings Settings) : CanonicalData;

public sealed record MixerState(ImmutableArray<MixerChannel> Channels, MixerStripSettings Master) : CanonicalData
{
    public bool ProtectOutput { get; init; }
}

public static class MixerValidation
{
    public const int MaximumChannels = 64;

    internal static void Validate(ProjectState state, HashSet<Guid> identities)
    {
        if (state.Mixer is not { } mixer) return;
        ProjectValidation.Require(!mixer.Channels.IsDefault && mixer.Channels.Length <= MaximumChannels,
            "Mixer supports up to 64 channels.");
        var ids = mixer.Channels.Select(c => c.Id).ToHashSet();
        var assigned = new HashSet<Id<SoundDefinition>>();
        var sounds = state.Sounds.Select(s => s.Id).ToHashSet();
        foreach (var c in mixer.Channels)
        {
            ProjectValidation.Require(c.Id.Value != Guid.Empty && identities.Add(c.Id.Value),
                "Invalid Mixer identity.");
            ProjectValidation.Require(
                !string.IsNullOrWhiteSpace(c.Name) && c.Name.Length <= ProjectValidation.MaximumTextLength,
                "Mixer channel name is required.");
            ProjectValidation.Require(c.ColorIndex is >= 0 and < 8 && !c.SoundIds.IsDefault && !c.Sends.IsDefault &&
                                      c.Sends.Length <= 8, "Invalid Mixer channel configuration.");
            foreach (var sound in c.SoundIds)
                ProjectValidation.Require(sounds.Contains(sound) && assigned.Add(sound),
                    "Mixer source is missing or assigned twice.");
            ProjectValidation.Require(c.Output is null || ids.Contains(c.Output.Value), "Mixer output is missing.");
            ProjectValidation.Require(c.Sends.Select(s => s.Target).Distinct().Count() == c.Sends.Length,
                "Duplicate Mixer send.");
            foreach (var send in c.Sends)
                ProjectValidation.Require(ids.Contains(send.Target) && send.LevelDb is >= -90 and <= 12,
                    "Invalid Mixer send.");
            Settings(c.Settings);
        }

        Settings(mixer.Master);
        _ = Order(mixer);
    }

    public static ImmutableArray<MixerChannel> Order(MixerState mixer)
    {
        var remaining = mixer.Channels.ToDictionary(c => c.Id);
        var result = ImmutableArray.CreateBuilder<MixerChannel>();
        while (remaining.Count > 0)
        {
            var next = remaining.Values.FirstOrDefault(c => !remaining.Values.Any(source =>
                source.Output == c.Id || source.Sends.Any(s => s.Target == c.Id)));
            ProjectValidation.Require(next is not null, "Mixer routing cycle is refused.");
            result.Add(next);
            remaining.Remove(next.Id);
        }

        return result.ToImmutable();
    }

    private static void Settings(MixerStripSettings s)
    {
        ProjectValidation.Require(s is not null && s.Eq is not null && s.Compressor is not null,
            "Mixer settings are required.");
        ProjectValidation.Require(
            s.TrimDb is >= -24 and <= 36 && s.LevelDb is >= -90 and <= 12 && s.Pan is >= -1 and <= 1,
            "Mixer level or pan is outside its range.");
        var e = s.Eq;
        ProjectValidation.Require((e.HighPassHz == 0 || e.HighPassHz is >= 20 and <= 2000) &&
                                  e.LowDb is >= -18 and <= 18 && e.MidDb is >= -18 and <= 18 &&
                                  e.HighDb is >= -18 and <= 18 &&
                                  e.MidHz is >= 40 and <= 16000 && e.MidQ is >= .2m and <= 10, "Invalid Mixer EQ.");
        var c = s.Compressor;
        ProjectValidation.Require(c.ThresholdDb is >= -60 and <= 0 && c.Ratio is >= 1 and <= 20 &&
                                  c.AttackMs is >= .1m and <= 200 && c.ReleaseMs is >= 10 and <= 2000 &&
                                  c.KneeDb is >= 0 and <= 24 && c.MakeupDb is >= -12 and <= 24,
            "Invalid Mixer compressor.");
    }

    internal static bool Same(MixerState? a, MixerState? b) => a == b || a is not null && b is not null &&
        a with { Channels = b.Channels } == b && a.Channels.Length == b.Channels.Length &&
        a.Channels.Zip(b.Channels).All(p =>
            p.First with { SoundIds = p.Second.SoundIds, Sends = p.Second.Sends } == p.Second &&
            p.First.SoundIds.SequenceEqual(p.Second.SoundIds) && p.First.Sends.SequenceEqual(p.Second.Sends));

    internal static bool Supported(MixerState mixer) => mixer.AdditionalData.IsEmpty &&
                                                        mixer.Channels.All(c =>
                                                            c.AdditionalData.IsEmpty &&
                                                            c.Sends.All(s => s.AdditionalData.IsEmpty) &&
                                                            Known(c.Settings)) &&
                                                        Known(mixer.Master);

    private static bool Known(MixerStripSettings s) => s.AdditionalData.IsEmpty && s.Eq.AdditionalData.IsEmpty &&
                                                       s.Compressor.AdditionalData.IsEmpty;
}
