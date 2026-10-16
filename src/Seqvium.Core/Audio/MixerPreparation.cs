// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

internal sealed record MixerTap(int Buffer, int Channel, double Gain);

internal sealed record MixerRoute(int Target, double Gain, bool PreFader);

internal sealed record PreparedMixerStrip(
    Guid Id,
    MixerStripSettings Settings,
    bool Audible,
    int Output,
    MixerRoute[] Sends,
    BiquadCoefficients[] Filters,
    double Attack,
    double Release,
    double Trim,
    double Gain,
    double Left,
    double Right,
    double Makeup,
    PreparedDelay Delay);

internal sealed record PreparedMixer(PreparedMixerStrip[] Strips, MixerTap[] Taps, bool ProtectOutput, int SampleRate,
    long TailFrames)
{
    internal int DelayBytes => checked(Strips.Length * MixerDelayDsp.StorageBytes(SampleRate));
}

internal static class MixerPreparation
{
    internal static PreparedMixer? Compile(ProjectState state, PreparedGraph graph, int sampleRate)
    {
        if (state.Mixer is not { } mixer) return null;
        if (!MixerValidation.Supported(mixer)) throw new GraphPreparationException("mixer.unknown-state");
        var ordered = MixerValidation.Order(mixer);
        var indices = ordered.Select((c, i) => (c.Id, Index: i)).ToDictionary(p => p.Id, p => p.Index);
        int master = ordered.Length;
        if ((master + 1L) * MixerDelayDsp.StorageBytes(sampleRate) > MixerDelayDsp.MaximumBytes)
            throw new GraphPreparationException(GraphPreparationReasons.Budget);
        var tails = new long[master + 1];
        foreach (var channel in ordered)
        {
            int index = indices[channel.Id];
            tails[index] += MixerDelayDsp.TailFrames(channel.Settings.Delay, sampleRate);
            int output = channel.Output is { } target ? indices[target] : master;
            tails[output] = Math.Max(tails[output], tails[index]);
            foreach (var send in channel.Sends)
                tails[indices[send.Target]] = Math.Max(tails[indices[send.Target]], tails[index]);
        }
        tails[master] += MixerDelayDsp.TailFrames(mixer.Master.Delay, sampleRate);
        if (tails[master] > sampleRate * 120L)
            throw new GraphPreparationException(GraphPreparationReasons.Budget);
        var sounds = ordered.SelectMany(c => c.SoundIds.Select(s => (Sound: s, Index: indices[c.Id])))
            .ToDictionary(p => p.Sound, p => p.Index);
        var partSounds = state.Patterns.SelectMany(p => p.Parts).ToDictionary(p => p.Id, p => p.SoundId);
        // Source/Gain/Mix are linear. Compute each source's exact transfer to the original graph output,
        // including branching, repeated paths and all local gains. No division by a possibly-zero gain.
        var taps = new List<MixerTap>();
        foreach (var contribution in graph.Contributions)
        {
            int source = graph.NodeBuffers[contribution.NodeId];
            var weights = new double[graph.BufferCount];
            weights[source] = 1;
            foreach (var op in graph.Operations)
            {
                if (op.Kind == GraphOperationKind.Source) continue;
                double gain = op.Kind == GraphOperationKind.Gain ? graph.Coefficients[op.Coefficient] : 1;
                weights[op.Buffer] = op.Inputs.Sum(input => weights[input]) * gain;
            }

            taps.Add(new(source,
                sounds.GetValueOrDefault(contribution.AudioSoundId ?? partSounds[contribution.PartId], master),
                weights[graph.OutputBuffer]));
        }

        // Solo opens its ancestors and descendants. An explicitly muted ancestor still cuts the path.
        var audible = new HashSet<Id<MixerChannel>>(ordered.Where(c => c.Settings.Solo).Select(c => c.Id));
        bool solo = audible.Count > 0;
        if (solo)
        {
            var forward = new HashSet<Id<MixerChannel>>(audible);
            foreach (var c in ordered)
                if (forward.Contains(c.Id))
                {
                    if (c.Output is { } output) forward.Add(output);
                    foreach (var send in c.Sends) forward.Add(send.Target);
                }

            var backward = new HashSet<Id<MixerChannel>>(audible);
            foreach (var c in ordered.Reverse())
                if (c.Output is { } output && backward.Contains(output) ||
                    c.Sends.Any(s => backward.Contains(s.Target)))
                    backward.Add(c.Id);
            audible.UnionWith(forward);
            audible.UnionWith(backward);
        }

        if (solo) taps = taps.Select(t => t.Channel == master ? t with { Gain = 0 } : t).ToList();

        PreparedMixerStrip Strip(Guid id, MixerStripSettings settings, bool allow, int output, MixerRoute[] sends) =>
            new(id, settings, allow && !settings.Muted, output, sends, MixerDsp.Filters(settings.Eq, sampleRate),
                Math.Exp(-1 / ((double)settings.Compressor.AttackMs * .001 * sampleRate)),
                Math.Exp(-1 / ((double)settings.Compressor.ReleaseMs * .001 * sampleRate)),
                MixerDsp.Amplitude(settings.TrimDb),
                allow && !settings.Muted ? MixerDsp.Amplitude(settings.LevelDb) : 0,
                settings.Pan > 0 ? Math.Cos((double)settings.Pan * Math.PI / 2) : 1,
                settings.Pan < 0 ? Math.Cos((double)settings.Pan * Math.PI / 2) : 1,
                settings.Compressor.Enabled ? MixerDsp.Amplitude(settings.Compressor.MakeupDb) : 1,
                MixerDelayDsp.Prepare(settings.Delay, sampleRate));

        return new([
            .. ordered.Select(c => Strip(c.Id.Value, c.Settings, !solo || audible.Contains(c.Id),
                c.Output is { } output ? indices[output] : master,
                [
                    .. c.Sends.Select(s => new MixerRoute(indices[s.Target], MixerDsp.Amplitude(s.LevelDb), s.PreFader))
                ])),
            Strip(Guid.Empty, mixer.Master, true, -1, [])
        ], [.. taps], mixer.ProtectOutput, sampleRate, tails[master]);
    }

    internal static bool SameTopology(MixerState? a, MixerState? b) => a is not null && b is not null &&
                                                                       a.Channels.Length == b.Channels.Length && a
                                                                           .Channels.Zip(b.Channels).All(p =>
                                                                               p.First.Id == p.Second.Id &&
                                                                               p.First.Output == p.Second.Output &&
                                                                               p.First.SoundIds.SequenceEqual(
                                                                                   p.Second.SoundIds) &&
                                                                               p.First.Sends
                                                                                   .Select(s => (s.Target, s.PreFader))
                                                                                   .SequenceEqual(
                                                                                       p.Second.Sends.Select(s =>
                                                                                           (s.Target, s.PreFader))));
}
