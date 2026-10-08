// SPDX-License-Identifier: Apache-2.0
using System.Numerics;
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class OfflineSamplerTests
{
    private const float Tolerance = 0.000002f;
    private sealed record TestNote(long Position, long Duration, decimal Pitch = 60m, decimal Intensity = 1m);

    [Theory]
    [InlineData(44100, 48000, 60, 60, 0.91875)]
    [InlineData(48000, 44100, 60, 60, 1.0884353741496599)]
    [InlineData(48000, 48000, 60, 72, 2)]
    [InlineData(48000, 48000, 60, 48, 0.5)]
    [InlineData(48000, 48000, 60, 66, 1.4142135623730951)]
    [InlineData(48000, 48000, 60, 60.5, 1.029302236643492)]
    [InlineData(48000, 48000, 60.5, 60.5, 1)]
    public void PitchRatioUsesSourceExecutionRatesRootAndFractionalSemitones(int source, int execution, double root, double pitch, double expected) =>
        Assert.Equal(expected, PcmSampler.Step(source, execution, (decimal)root, (decimal)pitch), 12);

    [Theory]
    [InlineData(60, 1, 1)]
    [InlineData(72, 2, 0.5)]
    [InlineData(48, 0.5, 0.25)]
    [InlineData(60.5, 1.029302236643492, 0.75)]
    public async Task RealPcmRampHasIndependentInterpolationAndVelocityOracle(double pitch, double step, double gain)
    {
        using var directory = new TemporaryDirectory();
        float[] samples = Enumerable.Range(0, 32).Select(i => i / 32f).ToArray();
        var fixture = await Create(directory, samples, [new(0, 320, (decimal)pitch, (decimal)gain)]);
        using var plan = SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern, 48000, channels: 1);
        float[] output = Render(plan, [3, 1, 5]);
        for (int frame = 0; frame < 8; frame++) Near((float)(frame * step / 32 * gain), output[frame]);
    }

    [Theory]
    [InlineData(44100, 48000)]
    [InlineData(48000, 44100)]
    public async Task ResamplingPcmProducesExpectedRampAcrossDeclaredRates(int sourceRate, int executionRate)
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory, Enumerable.Range(0, 32).Select(i => i / 32f).ToArray(), [new(0, 400)], sourceRate: sourceRate);
        using var plan = SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern, executionRate, channels: 1);
        var output = Render(plan, [1, 4]);
        for (int frame = 0; frame < 8; frame++) Near((float)((double)frame * sourceRate / executionRate / 32), output[frame]);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    [InlineData(2, 2)]
    public async Task ChannelConversionPreservesStereoOrderingAndDuplicatesMono(int sourceChannels, int outputChannels)
    {
        using var directory = new TemporaryDirectory();
        float[] samples = sourceChannels == 1 ? Enumerable.Repeat(0.25f, 32).ToArray() :
            Enumerable.Range(0, 32).SelectMany(_ => new[] { 0.75f, -0.25f }).ToArray();
        var fixture = await Create(directory, samples, [new(0, 160)], sourceChannels: sourceChannels);
        using var plan = SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern, 48000, channels: outputChannels);
        var output = Render(plan, [3]);
        Near(outputChannels == 1 || sourceChannels == 1 ? 0.25f : 0.75f, output[0]);
        if (outputChannels == 2) Near(sourceChannels == 1 ? 0.25f : -0.25f, output[1]);
    }

    [Fact]
    public async Task NaturalEndInterpolatesTowardZeroAndRetiresWithoutWaitingForNoteOff()
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory, [1f, 0.5f], [new(0, 400, 48m)]);
        using var plan = SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern, 48000, channels: 1);
        using var execution = plan.CreateExecution();
        float[] first = new float[4]; execution.Process(first);
        Near([1, 0.75f, 0.5f, 0.25f], first); Assert.Equal(0, execution.ActiveVoices);
        float[] rest = new float[(int)(plan.EndFrame - execution.Position)]; execution.Process(rest);
        Assert.All(rest, sample => Assert.Equal(0, sample));
    }

    [Fact]
    public async Task FiniteDurationAndLinearReleaseHaveIndependentEnvelopeOracle()
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory, Enumerable.Repeat(1f, 64).ToArray(), [new(0, 160)], release: 0.0625m, length: 320);
        using var plan = SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern, 48000, channels: 1);
        var output = Render(plan, [2, 3, 1]);
        Near([1, 1, 1, 1, 1, 2f / 3, 1f / 3, 0, 0, 0, 0], output);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]
    public async Task DeclaredVoiceConfigurationsMixIndependentNotesWithoutSilentLoss(int voices)
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory, Enumerable.Repeat(1f, 32).ToArray(),
            Enumerable.Range(0, voices).Select(_ => new TestNote(0, 160, Intensity: 0.1m)).ToArray());
        using var plan = SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern, 48000, channels: 1, voiceCapacity: voices);
        using var execution = plan.CreateExecution();
        float[] output = new float[1]; execution.Process(output);
        Assert.Equal(voices, execution.ActiveVoices); Near(voices * 0.1f, output[0]);
        Assert.Single(plan.Events.Where(item => item.Kind == ExecutionEventKind.NoteOn).Select(item => item.Occurrence?.PatternId).Distinct());
        Assert.Equal(voices, plan.Events.Where(item => item.Kind == ExecutionEventKind.NoteOn).Select(item => item.Occurrence).Distinct().Count());
    }

    [Fact]
    public async Task ReleasingOneOccurrenceNeverTerminatesAnotherUseOfTheSameSound()
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory, Enumerable.Repeat(1f, 64).ToArray(),
            [new(0, 80, Intensity: 0.2m), new(0, 240, Intensity: 0.2m)], release: 1m / 24, length: 360);
        using var plan = SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern, 48000, channels: 1);
        var output = Render(plan, [1, 2]);
        Near([0.4f, 0.4f, 0.4f, 0.3f, 0.2f, 0.2f, 0.2f, 0.1f, 0, 0, 0], output);
    }

    [Fact]
    public async Task SameFrameReleasePrecedesNextStartAndAllowsCapacityReuse()
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory, Enumerable.Repeat(1f, 32).ToArray(),
            [new(0, 80, Intensity: 0.25m), new(80, 80, Intensity: 0.75m)], length: 200);
        using var plan = SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern, 48000, channels: 1, voiceCapacity: 1);
        Assert.Equal(new[] { ExecutionEventKind.NoteOff, ExecutionEventKind.NoteOn },
            plan.Events.Where(item => item.Frame == 2).Select(item => item.Kind));
        Near([0.25f, 0.25f, 0.75f, 0.75f, 0], Render(plan, [2, 1]));
    }

    [Theory]
    [InlineData(44100, 120)]
    [InlineData(48000, 120)]
    [InlineData(44100, 137)]
    [InlineData(48000, 137)]
    public async Task AbsoluteLoopBoundariesMatchRationalOracleWithoutDriftAndBlocksDoNotDuplicate(int rate, int bpm)
    {
        using var directory = new TemporaryDirectory();
        long loop = MusicalPosition.TicksPerQuarter / 7; // Deliberately nonintegral execution-frame period.
        var fixture = await Create(directory, [0.5f, -0.5f, 0.25f], [new(0, loop)], bpm: bpm, length: loop);
        var start = new MusicalPosition(11 * MusicalPosition.TicksPerQuarter + 12345);
        using var plan = SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern, rate, channels: 1, start: start, repeats: 32);
        var ons = plan.Events.Where(item => item.Kind == ExecutionEventKind.NoteOn).ToArray();
        Assert.Equal(32, ons.Length);
        for (int iteration = 0; iteration < 32; iteration++)
        {
            long ticks = start.Ticks + iteration * loop;
            Assert.Equal(ticks, ons[iteration].Position.Ticks);
            Assert.Equal(OracleFrames(ticks, rate, bpm), ons[iteration].Frame);
            Assert.Equal(iteration, ons[iteration].Occurrence?.Iteration);
        }
        Assert.Equal(OracleFrames(start.Ticks + 32 * loop, rate, bpm), plan.EndFrame);
        var one = Render(plan, [int.MaxValue]);
        Assert.Equal(one, Render(plan, [1, 7, 31, 64, 3]));
        // Independent audible oracle: each absolute onset restarts source sample zero exactly once.
        foreach (var on in ons) Near(0.5f, one[on.Frame - plan.StartFrame]);
        Assert.All(one, sample => Assert.True(float.IsFinite(sample)));
    }

    [Fact]
    public async Task ZeroFramePositiveNotesAreExplicitlyInaudibleAndNeverBecomeStuckVoices()
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory, [1f, 1f], [new(0, 1), new(40, 1)], length: 160);
        using var plan = SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern, 48000, channels: 1, voiceCapacity: 1);
        Assert.Equal(2, plan.ZeroFrameNotes); Assert.Single(plan.Events); Assert.Equal(ExecutionEventKind.Stop, plan.Events[0].Kind);
        using var execution = plan.CreateExecution(); float[] output = new float[4]; execution.Process(output);
        Assert.All(output, sample => Assert.Equal(0, sample)); Assert.Equal(0, execution.ActiveVoices);
        Assert.All(fixture.Document.Current.State.Patterns.Single().Parts.Single().Notes, note => Assert.Equal(1, note.Duration.Ticks));
    }

    [Fact]
    public async Task StopHasPriorityAtItsBoundaryAndRestartIsControlledAndDeterministic()
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory, Enumerable.Repeat(1f, 32).ToArray(), [new(0, 80), new(80, 80)], length: 240, release: 5m);
        using var plan = SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern, 48000, channels: 1, stop: new MusicalPosition(80));
        Assert.Equal(ExecutionEventKind.Stop, plan.Events.Last().Kind); Assert.Equal(2, plan.EndFrame);
        using var execution = plan.CreateExecution();
        float[] first = new float[1]; execution.Process(first); Near(1, first[0]);
        execution.Stop(); Assert.Equal(0, execution.ActiveVoices);
        float[] silent = new float[1]; execution.Process(silent); Near(0, silent[0]);
        execution.Restart(); float[] restarted = new float[2]; execution.Process(restarted);
        Near([1, 1], restarted); Assert.Equal(0, execution.ActiveVoices);
        Assert.Throws<ArgumentOutOfRangeException>(() => execution.Process(new float[1]));
        execution.Dispose(); Assert.Throws<ObjectDisposedException>(() => execution.Restart());
    }

    [Fact]
    public async Task PreparedAndLiveSourcesHaveIndependentLifetimesAfterDocumentCloseAndOwnerDisposal()
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory, [0.5f, -0.5f, 0.25f], [new(0, 160)]);
        var plan = SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern, 48000, channels: 1);
        using var execution = plan.CreateExecution(); plan.Dispose(); fixture.Document.Close();
        float[] output = new float[4]; execution.Process(output); Near([0.5f, -0.5f, 0.25f, 0], output);
        Assert.Throws<ObjectDisposedException>(() => plan.CreateExecution());
    }

    [Theory]
    [InlineData("too-many-voices")]
    [InlineData("release-overlap")]
    [InlineData("capacity-config")]
    [InlineData("pitch")]
    [InlineData("rate")]
    [InlineData("algorithm")]
    [InlineData("missing-resource")]
    [InlineData("unknown-parameter")]
    [InlineData("event-budget")]
    [InlineData("repeat-limit")]
    [InlineData("route")]
    public async Task UnsupportedScopeIsReportedBeforeExecutionWithoutDroppingNotes(string failure)
    {
        using var directory = new TemporaryDirectory();
        TestNote[] notes = failure == "too-many-voices" ? Enumerable.Range(0, 9).Select(_ => new TestNote(0, 160)).ToArray() :
            failure == "event-budget" ? Enumerable.Range(0, 50).Select(_ => new TestNote(0, 160)).ToArray() :
            failure == "release-overlap" ? [new(0, 80), new(80, 80)] : [new(0, 160, failure == "pitch" ? 72.5m : 60m)];
        var fixture = await Create(directory, Enumerable.Repeat(1f, 64).ToArray(), notes, release: failure == "release-overlap" ? 10 : 0);
        if (failure == "algorithm") fixture.Document.Edit("Unsupported source", edit =>
        {
            var other = edit.AddSound("Other", "unimplemented");
            edit.AddPart(fixture.Pattern, "Unsupported part", other);
        });
        if (failure == "unknown-parameter") fixture.Document.Edit("Unknown processing", edit => edit.SetSoundParameters(fixture.Sound,
            fixture.Document.Current.State.Sounds.Single().Parameters.SetItem("filter", 1)));
        if (failure == "route") fixture.Document.Edit("Required route", edit =>
        {
            var placement = edit.AddPlacement(fixture.Pattern, new(0));
            var route = edit.AddRoute("Required route");
            edit.SetPlacementRelationships(placement, null, null, route, []);
        });
        if (failure == "missing-resource")
            foreach (var path in Directory.GetFiles(directory.File("owned"), "*.wav", SearchOption.AllDirectories)) File.Delete(path);
        var before = fixture.Document.Current;
        var error = Record.Exception(() => SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern,
            failure == "rate" ? 96000 : 48000, voiceCapacity: failure == "capacity-config" ? 9 : failure == "release-overlap" ? 1 : 8,
            repeats: failure == "event-budget" ? 1024 : failure == "repeat-limit" ? 1025 : 1));
        Assert.True(error is NotSupportedException or IOException, $"Expected scope/availability failure; received {error?.GetType()}.");
        Assert.Same(before, fixture.Document.Current);
    }

    [Theory]
    [InlineData("pitch")]
    [InlineData("missing-resource")]
    public async Task ZeroFrameQuantizationDoesNotHideUnsupportedPitchOrMissingResource(string failure)
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory, [0.5f], [new(0, 1, failure == "pitch" ? 73m : 60m)], length: 160);
        if (failure == "missing-resource")
            foreach (var path in Directory.GetFiles(directory.File("owned"), "*.wav", SearchOption.AllDirectories)) File.Delete(path);
        var error = Record.Exception(() => SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern, 48000));
        Assert.True(error is IOException or NotSupportedException);
    }

    [Fact]
    public async Task PreparationAtAnotherRatePreservesCanonicalMusicalIntentAndFrozenRevision()
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory, [0.5f, -0.5f], [new(12345, 54321, 60.5m)], bpm: 137);
        var bytes = ProjectPersistence.Encode(fixture.Document); var revision = fixture.Document.Current.Revision;
        using var first = SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern, 44100);
        using var second = SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern, 48000);
        Assert.Equal(bytes, ProjectPersistence.Encode(fixture.Document));
        Assert.Equal(revision, first.Revision); Assert.Equal(revision, second.Revision);
        Assert.Equal(first.Events.Select(item => item.Position), second.Events.Select(item => item.Position));
        Assert.NotEqual(first.Events[0].Frame, second.Events[0].Frame);
        fixture.Document.Edit("Later edit", edit => edit.RenameDocument("Changed"));
        Assert.Equal(revision, first.Revision); Assert.Equal(Render(first, [1, 7]), Render(first, [int.MaxValue]));
    }

    [Fact]
    public async Task EqualPositionStartsRetainPartThenNoteIdentityOrderingAndZeroIntensityIsSilent()
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory, Enumerable.Repeat(1f, 32).ToArray(), [new(0, 160, Intensity: 0m), new(0, 160, Intensity: 0m)]);
        fixture.Document.Edit("Second independent part of shared sound", edit =>
        {
            var second = edit.AddPart(fixture.Pattern, "Other use", fixture.Sound);
            edit.AddNote(fixture.Pattern, second, new(0), new(160), 60m, 0m);
        });
        using var plan = SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern, 48000, channels: 1);
        var expected = fixture.Document.Current.State.Patterns.Single().Parts.OrderBy(part => part.Id.Value)
            .SelectMany(part => part.Notes.OrderBy(note => note.Id.Value).Select(note => (part.Id, note.Id))).ToArray();
        var actual = plan.Events.Where(item => item.Kind == ExecutionEventKind.NoteOn)
            .Select(item => (item.Occurrence!.Value.PartId, item.Occurrence.Value.NoteId)).ToArray();
        Assert.Equal(expected, actual); Assert.All(Render(plan, [1, 3]), sample => Assert.Equal(0, sample));
    }

    private static async Task<(ProjectDocument Document, Id<Pattern> Pattern, Id<MusicalPart> Part, Id<SoundDefinition> Sound)>
        Create(TemporaryDirectory directory, float[] samples, TestNote[] notes, decimal release = 0,
            long? length = null, int sourceRate = 48000, int sourceChannels = 1, int bpm = 120)
    {
        var document = ProjectDocument.Create(new(new Tempo(bpm), new Meter(4, 4)));
        var accepted = await WavFixtures.Import(document, directory, samples, sourceRate, sourceChannels, release: release);
        Id<Pattern> pattern = default; Id<MusicalPart> part = default;
        document.Edit("Fixture music", edit =>
        {
            pattern = edit.AddPattern("Music", new(length ?? notes.Max(note => note.Position + note.Duration)));
            part = edit.AddPart(pattern, "Sample part", accepted.SoundId);
            foreach (var note in notes) edit.AddNote(pattern, part, new(note.Position), new(note.Duration), note.Pitch, note.Intensity);
        });
        return (document, pattern, part, accepted.SoundId);
    }
    private static float[] Render(PreparedSampler plan, int[] partitions)
    {
        using var execution = plan.CreateExecution();
        var output = new float[checked((int)(plan.EndFrame - plan.StartFrame) * plan.Channels)];
        int offset = 0, partition = 0;
        while (execution.Position < plan.EndFrame)
        {
            int frames = (int)Math.Min(partitions[partition++ % partitions.Length], plan.EndFrame - execution.Position);
            execution.Process(output.AsSpan(offset, frames * plan.Channels)); offset += frames * plan.Channels;
        }
        Assert.Equal(0, execution.ActiveVoices);
        return output;
    }
    private static long OracleFrames(long ticks, int rate, int bpm)
    {
        BigInteger numerator = (BigInteger)ticks * 60 * rate;
        BigInteger denominator = (BigInteger)960000 * bpm;
        return (long)((2 * numerator + denominator) / (2 * denominator));
    }
    private static void Near(float expected, float actual) => Assert.InRange(Math.Abs(actual - expected), 0, Tolerance);
    private static void Near(float[] expected, float[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++) Near(expected[i], actual[i]);
    }
}
