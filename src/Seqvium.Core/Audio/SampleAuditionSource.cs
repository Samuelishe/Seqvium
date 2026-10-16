// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;

namespace Seqvium.Core;

/// <summary>Temporary shared PCM sound substitution in a prepared plan. Canonical media and graphs remain untouched.</summary>
internal sealed class SampleAuditionSource : IDisposable
{
    private readonly PcmLease _pcm;
    internal Id<SoundDefinition> Sound { get; }
    internal Guid Candidate { get; }
    internal SampleAuditionSource(Id<SoundDefinition> sound, Guid candidate, DecodedPcm pcm)
    {
        if (candidate == Guid.Empty || pcm.Frames > pcm.SampleRate * 2 || pcm.Channels is < 1 or > 2)
            throw new ArgumentException("Lab.ContextUnavailable");
        Sound = sound; Candidate = candidate; _pcm = pcm.Lease();
    }
    private SampleAuditionSource(SampleAuditionSource source)
    { Sound = source.Sound; Candidate = source.Candidate; _pcm = source._pcm.Lease(); }
    internal SampleAuditionSource Borrow() => new(this);
    internal PreparedSampler Apply(PreparedSampler original)
    {
        var state = original.CanonicalBasis ?? throw new InvalidOperationException("Lab.ContextUnavailable");
        var sound = state.Sounds.Single(s => s.Id == Sound);
        if (sound.Algorithm != PcmSampler.Algorithm || !BuiltInSound.Supports(sound))
            throw new InvalidOperationException("Lab.ContextUnavailable");
        var parts = state.Patterns.SelectMany(p => p.Parts).Where(p => p.SoundId == Sound).ToDictionary(p => p.Id);
        var sources = original.LeaseSources().ToList();
        try
        {
            int index = sources.Count;
            sources.Add(_pcm.Lease());
            var notes = original.Notes.Select(n =>
            {
                if (!parts.TryGetValue(n.Occurrence.PartId, out var part)) return n;
                var note = part.Notes.Single(note => note.Id == n.Occurrence.NoteId);
                return n with { SourceIndex = index, SourceOffset = 0, SourceLength = long.MaxValue,
                    SustainStart = 0, SustainEnd = 0, SustainFade = 0, Step = PcmSampler.Step(_pcm.Rate, original.SampleRate,
                    sound.Parameters["rootPitch"], note.Pitch) };
            }).ToImmutableArray();
            SamplerPreparation.ValidateCapacity(original.Events, notes.ToBuilder(), sources, original.VoiceCapacity, original.EndFrame);
            return new(original.Revision, original.SampleRate, original.Channels, original.VoiceCapacity,
                original.StartFrame, original.EndFrame, original.ZeroFrameNotes, notes, original.Events, [.. sources], original.Graph)
            {
                CanonicalBasis = state, Loop = original.Loop, IsArrangement = original.IsArrangement,
                SourceEndFrame = original.SourceEndFrame, AllowMixerTail = original.AllowMixerTail,
                AuditionSound = Sound, AuditionCandidate = Candidate
            };
        }
        catch { foreach (var source in sources) source.Dispose(); throw; }
    }
    public void Dispose() => _pcm.Dispose();
}
