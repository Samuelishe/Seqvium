// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class MusicalTimeTests
{
    [Theory]
    [InlineData(60, 44100, 44100)]
    [InlineData(120, 44100, 22050)]
    [InlineData(240, 44100, 11025)]
    [InlineData(60, 48000, 48000)]
    [InlineData(120, 48000, 24000)]
    [InlineData(240, 48000, 12000)]
    public void QuarterAndBarBoundaries(int bpm, int rate, long quarterFrames)
    {
        var tempo = new Tempo(bpm);
        Assert.Equal(0, ConstantTempoConversion.ToFrames(new(0), tempo, rate));
        Assert.Equal(quarterFrames, ConstantTempoConversion.ToFrames(new(MusicalPosition.TicksPerQuarter), tempo, rate));
        var meter = new Meter(4, 4);
        Assert.Equal(quarterFrames * 4, ConstantTempoConversion.ToFrames(meter.BarStart(1), tempo, rate));
        Assert.Equal(quarterFrames, ConstantTempoConversion.DurationFrames(new(0), SyntheticComposition.Quarter, tempo, rate));
        Assert.Equal(new MusicalPosition(MusicalPosition.TicksPerQuarter), ConstantTempoConversion.FromFrames(quarterFrames, tempo, rate));
    }

    [Theory]
    [InlineData(137, 44100)]
    [InlineData(137, 48000)]
    [InlineData(1, 44100)]
    [InlineData(1000, 48000)]
    public void AbsoluteBoundariesDoNotAccumulateRoundedDurations(int bpm, int rate)
    {
        var tempo = new Tempo(bpm);
        var step = new MusicalDuration(MusicalPosition.TicksPerQuarter / 3);
        long accumulated = 0;
        for (int index = 0; index < 20_000; index++)
        {
            var start = new MusicalPosition(index * step.Ticks);
            accumulated += ConstantTempoConversion.DurationFrames(start, step, tempo, rate);
            Assert.Equal(ConstantTempoConversion.ToFrames(start.Add(step), tempo, rate), accumulated);
        }
        var absolute = ConstantTempoConversion.ToFrames(new(20_000 * step.Ticks), tempo, rate);
        Assert.Equal(absolute, accumulated);
        if (bpm == 137)
            Assert.NotEqual(absolute, 20_000 * ConstantTempoConversion.DurationFrames(new(0), step, tempo, rate));
    }

    [Fact]
    public void ExactHalfFramesRoundLaterAndDurationUsesTwoRoundedEndpoints()
    {
        var tempo = new Tempo(120m);
        Assert.Equal(0, ConstantTempoConversion.ToFrames(new(19), tempo, 48000));
        Assert.Equal(1, ConstantTempoConversion.ToFrames(new(20), tempo, 48000));
        Assert.Equal(1, ConstantTempoConversion.ToFrames(new(59), tempo, 48000));
        Assert.Equal(2, ConstantTempoConversion.ToFrames(new(60), tempo, 48000));
        Assert.Equal(1, ConstantTempoConversion.DurationFrames(new(19), new(1), tempo, 48000));
        Assert.Equal(0, ConstantTempoConversion.DurationFrames(new(20), new(1), tempo, 48000));
        // One frame equals half a tick here: verify reverse rounding independently.
        Assert.Equal(1, ConstantTempoConversion.FromFrames(1, new Tempo(60m), 1_920_000).Ticks);
    }

    [Fact]
    public void FractionalTempoAndLongProjectsRemainExactWithoutFloatingPointOverflow()
    {
        var tempo = new Tempo(123.456789m);
        var position = new MusicalPosition(1_000_000_000L * MusicalPosition.TicksPerQuarter);
        Assert.Equal(21_432_600_195_037L, ConstantTempoConversion.ToFrames(position, tempo, 44100));
        Assert.Equal(23_328_000_212_285L, ConstantTempoConversion.ToFrames(position, tempo, 48000));
        var maximum = new MusicalPosition(long.MaxValue);
        Assert.True(ConstantTempoConversion.ToFrames(maximum, new Tempo(120m), 48000) > 0);
        Assert.Throws<OverflowException>(() => maximum.Add(new(1)));
        Assert.Throws<OverflowException>(() => ConstantTempoConversion.ToFrames(maximum, new Tempo(1m), int.MaxValue));
    }

    [Theory]
    [InlineData(44100)]
    [InlineData(48000)]
    public void RepeatedFrameRoundTripStabilizesWithoutCumulativeDrift(int rate)
    {
        var tempo = new Tempo(137.125m);
        var originalFrames = ConstantTempoConversion.ToFrames(new(12_345_678_901), tempo, rate);
        var current = originalFrames;
        for (int index = 0; index < 1000; index++)
        {
            current = ConstantTempoConversion.ToFrames(ConstantTempoConversion.FromFrames(current, tempo, rate), tempo, rate);
            Assert.Equal(originalFrames, current);
        }
    }

    [Fact]
    public void MeterUsesQuarterNoteTempoAndSupportsDifferentBarBoundaries()
    {
        var threeFour = new Meter(3, 4);
        var sevenEight = new Meter(7, 8);
        Assert.Equal(3 * MusicalPosition.TicksPerQuarter, threeFour.BarStart(1).Ticks);
        Assert.Equal(7 * MusicalPosition.TicksPerQuarter / 2, sevenEight.BarStart(1).Ticks);
        Assert.Equal(168_000, ConstantTempoConversion.ToFrames(sevenEight.BarStart(2), new Tempo(120m), 48000));
        Assert.Throws<OverflowException>(() => sevenEight.BarStart(long.MaxValue));
    }

    [Fact]
    public void InvalidTimeTempoAndMeterAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MusicalPosition(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MusicalDuration(0));
        foreach (var bpm in new[] { 0m, 1001m, 120.0000001m })
            Assert.Throws<ArgumentOutOfRangeException>(() => new Tempo(bpm));
        Assert.Throws<ArgumentOutOfRangeException>(() => ConstantTempoConversion.ToFrames(new(0), default, 48000));
        Assert.Throws<ArgumentOutOfRangeException>(() => ConstantTempoConversion.ToFrames(new(0), new Tempo(120m), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ConstantTempoConversion.DurationFrames(new(0), default, new Tempo(120m), 48000));
        var document = ProjectDocument.Create();
        Assert.Throws<ArgumentOutOfRangeException>(() => new Meter(4, 3));
        Assert.Throws<ProjectValidationException>(() => document.Edit("Invalid meter", edit => edit.SetSettings(new(new Tempo(120m), new Meter(4, 4) with { Denominator = 3 }))));
        Assert.Equal(0, document.UndoCount);
    }

    [Fact]
    public void EqualPositionOrderingUsesStablePartAndNoteIdentities()
    {
        var fixture = new SyntheticComposition();
        fixture.Document.Edit("Same-position notes", edit =>
        {
            edit.AddNote(fixture.Pattern, fixture.BassPart, new(0), SyntheticComposition.Quarter, 60m, 1m);
            edit.AddNote(fixture.Pattern, fixture.KickPart, new(0), SyntheticComposition.Quarter, 42m, 1m);
        });
        var pattern = fixture.SharedPattern;
        var expected = pattern.OrderedNotes().Select(item => (item.PartId, item.Note.Id)).ToArray();
        var reordered = pattern with { Parts = [.. pattern.Parts.Reverse().Select(part => part with { Notes = [.. part.Notes.Reverse()] })] };
        Assert.Equal(expected, reordered.OrderedNotes().Select(item => (item.PartId, item.Note.Id)));
        Assert.Equal(expected.OrderBy(item => pattern.Parts.Single(part => part.Id == item.PartId).Notes.Single(note => note.Id == item.Id).Position.Ticks)
            .ThenBy(item => item.PartId.Value).ThenBy(item => item.Id.Value), expected);
    }
}
