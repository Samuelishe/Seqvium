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
    PreparedDelay Delay, PreparedReverb Reverb)
{
    internal long HistoryDelayFrames { get; init; } = Settings.Delay.Mix > 0 ? Delay.TailFrames : 0;
    internal long HistoryReverbFrames { get; init; } = Reverb.TailFrames;
    internal bool HasHistory { get; init; }
}

internal sealed record PreparedMixer(PreparedMixerStrip[] Strips, MixerTap[] Taps, bool ProtectOutput, int SampleRate,
    long TailFrames)
{
    internal int ReverbBytes => checked(Strips.Count(s => s.Reverb.Admitted) * MixerReverbDsp.StorageBytes(SampleRate));
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
                MixerDelayDsp.Prepare(settings.Delay, sampleRate), MixerReverbDsp.Prepare(settings.Reverb, sampleRate));

        PreparedMixerStrip[] strips = [
            .. ordered.Select(c => Strip(c.Id.Value, c.Settings, !solo || audible.Contains(c.Id),
                c.Output is { } output ? indices[output] : master,
                [
                    .. c.Sends.Select(s => new MixerRoute(indices[s.Target], MixerDsp.Amplitude(s.LevelDb), s.PreFader))
                ])),
            Strip(Guid.Empty, mixer.Master, true, -1, [])
        ];
        if ((long)strips.Count(s => s.Reverb.Enabled) * MixerReverbDsp.StorageBytes(sampleRate) > MixerReverbDsp.MaximumBytes)
            throw new GraphPreparationException(GraphPreparationReasons.Budget);
        long extension = TailFrames(strips, [.. taps], sampleRate, inherit: false);
        return new(strips, [.. taps], mixer.ProtectOutput, sampleRate, extension);
    }

    private static long TailFrames(PreparedMixerStrip[] strips, MixerTap[] taps, int sampleRate, bool inherit)
    {
        // Fresh renders seed only real source paths. Live continuations additionally seed processor
        // histories that could already be excited, even after their canonical source is removed.
        var tails = Enumerable.Repeat(-1L, strips.Length).ToArray();
        foreach (var tap in taps)
            if (tap.Gain != 0) tails[tap.Channel] = 0;
        if (inherit)
            for (int i = 0; i < strips.Length; i++)
                if (strips[i].HasHistory) tails[i] = Math.Max(0, tails[i]);
        for (int i = 0; i < strips.Length; i++)
        {
            var p = strips[i];
            if (tails[i] < 0 || !p.Audible || p.Trim == 0) { tails[i] = -1; continue; }
            long tail = checked(tails[i] + p.HistoryDelayFrames + p.HistoryReverbFrames);
            if (p.HistoryDelayFrames > 0 || p.HistoryReverbFrames > 0)
                strips[i] = p with { HasHistory = true };
            if (p.Output >= 0 && p.Gain != 0) tails[p.Output] = Math.Max(tails[p.Output], tail);
            foreach (var send in p.Sends)
                if (send.Gain != 0 && (send.PreFader || p.Gain != 0))
                    tails[send.Target] = Math.Max(tails[send.Target], tail);
            tails[i] = p.Gain != 0 ? tail : -1;
        }
        long extension = Math.Max(0, tails[^1]);
        if (extension > sampleRate * 120L) throw new GraphPreparationException(GraphPreparationReasons.Budget);
        return extension;
    }

    internal static PreparedMixer ReserveContinuation(PreparedMixer next, PreparedMixer? previous)
    {
        if (previous is null) return next;
        var prior = previous.Strips.ToDictionary(s => s.Id);
        var strips = next.Strips.Select(s => prior.TryGetValue(s.Id, out var old)
            ? s with
            {
                Reverb = s.Reverb with { Admitted = s.Reverb.Admitted || old.Reverb.Admitted },
                HistoryDelayFrames = old.HasHistory ? Math.Max(s.HistoryDelayFrames, old.HistoryDelayFrames) : s.HistoryDelayFrames,
                HistoryReverbFrames = old.HasHistory ? Math.Max(s.HistoryReverbFrames, old.HistoryReverbFrames) : s.HistoryReverbFrames,
                HasHistory = s.HasHistory || old.HasHistory
            } : s).ToArray();
        if ((long)strips.Count(s => s.Reverb.Admitted) * MixerReverbDsp.StorageBytes(next.SampleRate) > MixerReverbDsp.MaximumBytes)
            throw new GraphPreparationException(GraphPreparationReasons.Budget);
        return next with { Strips = strips, TailFrames = TailFrames(strips, next.Taps, next.SampleRate, inherit: true) };
    }

    internal static bool SameTopology(MixerState? a, MixerState? b) => a is not null && b is not null &&
                                                                       a.Channels.Length == b.Channels.Length &&
                                                                       ReverbCompatible(a.Master.Reverb, b.Master.Reverb) && a
                                                                           .Channels.Zip(b.Channels).All(p =>
                                                                               p.First.Id == p.Second.Id &&
                                                                               ReverbCompatible(p.First.Settings.Reverb, p.Second.Settings.Reverb) &&
                                                                               p.First.Output == p.Second.Output &&
                                                                               p.First.SoundIds.SequenceEqual(
                                                                                   p.Second.SoundIds) &&
                                                                               p.First.Sends
                                                                                   .Select(s => (s.Target, s.PreFader))
                                                                                   .SequenceEqual(
                                                                                       p.Second.Sends.Select(s =>
                                                                                           (s.Target, s.PreFader))));
    // Enabling and geometry changes need worker-prepared exclusive storage. Bypass is a coefficient
    // update: the admitted bank remains owned/accounted by the active execution until retirement.
    private static bool ReverbCompatible(MixerReverb a, MixerReverb b) =>
        (!b.Enabled || a.Enabled) && a.Size == b.Size;

}
