// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

public sealed partial class ProjectEdit
{
    /// <summary>Explicit opt-in. Equal part names are grouped, with stable sound assignments visible in the Mixer.</summary>
    public void InitializeMixer()
    {
        CheckActive();
        if (State.Mixer is not null) return;
        var groups = State.Patterns.SelectMany(p => p.Parts).GroupBy(p => p.Name, StringComparer.Ordinal).ToArray();
        var assigned = new HashSet<Id<SoundDefinition>>();
        var channels = ImmutableArray.CreateBuilder<MixerChannel>();
        foreach (var group in groups)
        {
            var sources = group.Select(p => p.SoundId).Distinct().Where(assigned.Add).ToImmutableArray();
            if (!sources.IsEmpty)
                channels.Add(new(Id<MixerChannel>.New(), group.Key, channels.Count % 8, sources,
                    null, [], new()));
        }

        State = State with { Mixer = new(channels.ToImmutable(), new()) };
    }

    public void SetMixer(MixerState mixer)
    {
        CheckActive();
        ProjectValidation.Require(State.Mixer is null || MixerValidation.Supported(State.Mixer),
            "Unknown Mixer state cannot be overwritten.");
        State = State with { Mixer = mixer };
    }

    public void SetMixerStrip(Id<MixerChannel>? channel, MixerStripSettings settings)
    {
        CheckActive();
        var mixer = State.Mixer ?? throw new ProjectValidationException("Mixer is not initialized.");
        SetMixer(channel is { } id
            ? mixer with { Channels = Replace(mixer.Channels, c => c.Id == id, c => c with { Settings = settings }) }
            : mixer with { Master = settings });
    }

    public Id<MixerChannel> AddMixerBus(string name)
    {
        CheckActive();
        var mixer = State.Mixer ?? throw new ProjectValidationException("Mixer is not initialized.");
        var bus = new MixerChannel(Id<MixerChannel>.New(), name, mixer.Channels.Length % 8, [], null, [], new());
        SetMixer(mixer with { Channels = mixer.Channels.Add(bus) });
        return bus.Id;
    }

    public void AssignMixerSound(Id<SoundDefinition> sound, Id<MixerChannel>? target)
    {
        CheckActive();
        var mixer = State.Mixer ?? throw new ProjectValidationException("Mixer is not initialized.");
        ProjectValidation.Require(State.Sounds.Any(s => s.Id == sound) &&
                                  (target is null || mixer.Channels.Any(c => c.Id == target)),
            "Mixer assignment target is missing.");
        SetMixer(mixer with
        {
            Channels =
            [
                .. mixer.Channels.Select(c => c with
                {
                    SoundIds = c.Id == target ? c.SoundIds.Remove(sound).Add(sound) : c.SoundIds.Remove(sound)
                })
            ]
        });
    }

    public void RemoveMixerChannel(Id<MixerChannel> id)
    {
        CheckActive();
        var mixer = State.Mixer ?? throw new ProjectValidationException("Mixer is not initialized.");
        SetMixer(mixer with
        {
            Channels =
            [
                .. mixer.Channels.Where(c => c.Id != id).Select(c => c with
                {
                    Output = c.Output == id ? null : c.Output, Sends = [.. c.Sends.Where(s => s.Target != id)]
                })
            ]
        });
    }
}
