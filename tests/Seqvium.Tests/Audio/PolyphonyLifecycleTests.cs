// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed partial class PolyphonyTests
{
    [Fact]
    public async Task SmallerPreparedCapacityRefusesBeforeCallbackWhenSurvivingVoicesOwnHighSlots()
    {
        using var f = await Fixture.Create(64);
        var notes = Fixture.Notes(64).OrderBy(n => n.Id.Value).Select((n, i) =>
            n with { Duration = new(i < 32 ? Fixture.Gate : Fixture.Length) }).ToImmutableArray();
        f.Document.Edit("Short low slots, held high slots", e => e.SetPartNotes(f.Pattern, f.Part, notes));
        using var rt = new RealtimeSampler(48000, 2, 511);
        using (var initial = rt.BeginGraphPreparation(f.Document, f.Attachment.Id, 64))
        {
            await initial.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, initial.Publish(TestContext.Current.CancellationToken));
        }
        rt.Start(); rt.Process(new float[960]); Assert.Equal(32, rt.ActiveVoices);
        long epoch = rt.TransportEpoch, position = rt.RenderPosition;
        f.Document.Edit("Keep only the continuing notes", e => e.SetPartNotes(f.Pattern, f.Part, notes[32..]));
        using (var smaller = rt.BeginGraphPreparation(f.Document, f.Attachment.Id, 32))
        {
            smaller.PreservePlayback = true;
            await smaller.PrepareAsync(TestContext.Current.CancellationToken);
            var error = Assert.Throws<GraphPreparationException>(() => smaller.Publish(TestContext.Current.CancellationToken));
            Assert.Contains("cannot shrink", error.InnerException!.Message);
        }
        Assert.False(rt.HasPending); Assert.Equal(1, rt.LivePreparedStates);
        Assert.True(rt.Process(new float[128]));
        Assert.Equal(position + 64, rt.RenderPosition); Assert.Equal(epoch, rt.TransportEpoch);
        Assert.Equal(32, rt.ActiveVoices);
        rt.Panic(); rt.Process(new float[2]); Assert.Equal(0, rt.ActiveVoices);
    }

    [Theory]
    [InlineData(32)] [InlineData(64)]
    public async Task ReusedHolesKeepLowestSlotAndExactAscendingPcmSum(int count)
    {
        using var f = await Fixture.Create(count);
        f.Document.Edit("Staggered releases and reuse", e =>
        {
            var first = Fixture.Notes(count).Select((n, i) => n with
                { Duration = new(i % 2 == 0 ? Fixture.Gate : Fixture.Gate * 3) }).ToImmutableArray();
            e.SetPartNotes(f.Pattern, f.Part, first.AddRange(Fixture.Notes(count / 2, start: 17280)));
        });
        using var plan = f.Prepare(); var pcm = Render(plan, 127);
        var notes = f.Document.Current.State.Patterns.Single().Parts.Single().Notes
            .OrderBy(n => n.Position.Ticks).ThenBy(n => n.Id.Value).ToArray();
        var slots = new (NoteEvent Note, long On, long Off, long End)?[count];
        int next = 0;
        long Frame(long ticks) => (long)decimal.Round(ticks / 40m, 0, MidpointRounding.AwayFromZero);
        for (int frame = 0; frame < pcm.Length / 2; frame++)
        {
            for (int s = 0; s < slots.Length; s++)
                if (slots[s] is { } old && frame >= old.End) slots[s] = null;
            while (next < notes.Length && Frame(notes[next].Position.Ticks) == frame)
            {
                var note = notes[next++]; long off = Frame(note.Position.Ticks + note.Duration.Ticks);
                int slot = Array.FindIndex(slots, s => s is null);
                Assert.True(slot >= 0); slots[slot] = (note, frame, off, off + 144);
            }
            for (int channel = 0; channel < 2; channel++)
            {
                float oracle = 0;
                foreach (var value in slots)
                    if (value is { } voice)
                    {
                        double step = Math.Pow(2, (double)(voice.Note.Pitch - 60) / 12);
                        double cursor = (frame - voice.On) * step; int whole = (int)cursor;
                        double first = Fixture.Sample(whole, channel), second = Fixture.Sample(whole + 1, channel);
                        double sample = first + (second - first) * (cursor - whole);
                        double gain = (float)voice.Note.Intensity *
                            (frame < voice.Off ? 1 : 1 - (frame - voice.Off) / 144d);
                        oracle += (float)(sample * gain);
                    }
                Assert.Equal(oracle, pcm[frame * 2 + channel]);
            }
        }
    }

    [Theory]
    [InlineData(44100)] [InlineData(48000)]
    public async Task MaximumSongSeekRangeLoopPanicAndPreparedHistoryKeepVoiceOwnership(int rate)
    {
        using var f = await Fixture.Create(GraphPreparation.MaximumVoices);
        f.Document.Edit("Song", e => e.AddArrangementClip(f.Placement, default, 0, 3));
        using var rt = new RealtimeSampler(rate, 2, 511);
        async Task Publish(bool live)
        {
            using var r = rt.BeginArrangementPreparation(f.Document, live);
            await r.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, r.Publish(TestContext.Current.CancellationToken));
        }
        await Publish(false); rt.Start(); var block = new float[128]; rt.Process(block);
        long epoch = rt.TransportEpoch;
        int held = rt.ActiveVoices; Assert.Equal(GraphPreparation.MaximumVoices, held);
        f.Document.Edit("Future repeat note", e => e.AddNote(f.Pattern, f.Part, new(96000), new(4800), 72, .01m));
        await Publish(true); rt.Process(block);
        Assert.Equal(epoch, rt.TransportEpoch); Assert.Equal(held, rt.ActiveVoices);
        Assert.InRange(rt.LivePreparedStates, 2, 3);
        long retained = rt.RetainedPcmAndScratchBytes; rt.RetireCompleted();
        Assert.True(rt.RetainedPcmAndScratchBytes < retained);
        Assert.True(f.Document.Undo()); await Publish(true); rt.Process(block); rt.RetireCompleted();
        Assert.Equal(epoch, rt.TransportEpoch); Assert.Equal(held, rt.ActiveVoices);
        Assert.True(f.Document.Redo()); await Publish(true); rt.Process(block); rt.RetireCompleted();
        Assert.Equal(epoch, rt.TransportEpoch);
        // Cold destination reconstruction must include the complete chord, not just its first 64 slots.
        rt.SeekSong(1); rt.Process(block); Assert.Equal(held, rt.ActiveVoices);
        rt.SetSongLoop(0, rate / 100, true);
        for (int i = 0; i < 40; i++) { Assert.True(rt.Process(block)); rt.RetireCompleted(); }
        Assert.InRange(rt.ActiveVoices, 0, held); Assert.Equal(held, rt.PeakVoices);
        long stop = rt.Panic(); rt.Process(block);
        Assert.True(rt.StopAcknowledgment >= stop); Assert.Equal(0, rt.ActiveVoices);
        Assert.All(block, value => Assert.Equal(0, value));
        rt.TerminateExecution(); rt.RetireCompleted(); rt.Dispose();
        Assert.Equal(0, rt.LivePreparedStates); Assert.Equal(rt.PreparedStatesCreated, rt.PreparedStatesReleased);
    }

    [Theory]
    [InlineData("stale")] [InlineData("cancelled")] [InlineData("consumer-stale")]
    public async Task MaximumCandidateStaleCancelledAndRetiredSlotsReleaseAllResources(string kind)
    {
        using var f = await Fixture.Create(GraphPreparation.MaximumVoices);
        using var rt = new RealtimeSampler(48000, 2, 511);
        using (var first = rt.BeginGraphPreparation(f.Document, f.Attachment.Id, GraphPreparation.MaximumVoices))
        {
            await first.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, first.Publish(TestContext.Current.CancellationToken));
        }
        rt.Start(); rt.Process(new float[2]);
        using (var request = rt.BeginGraphPreparation(f.Document, f.Attachment.Id, GraphPreparation.MaximumVoices))
        {
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            if (kind == "stale")
            {
                rt.InvalidatePreparation(); Assert.Equal(SamplerPublication.Stale, request.Publish(TestContext.Current.CancellationToken));
            }
            else if (kind == "cancelled")
            {
                using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
                Assert.Equal(SamplerPublication.Cancelled, request.Publish(cancelled.Token));
            }
            else
            {
                Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
                rt.InvalidatePreparation(); rt.Process(new float[2]);
                Assert.True(rt.ConsumerStaleRejections > 0);
            }
        }
        Assert.Equal(GraphPreparation.MaximumVoices, rt.ActiveVoices);
        rt.RetireCompleted(); rt.TerminateExecution(); rt.RetireCompleted(); rt.Dispose();
        Assert.Equal(0, rt.LivePreparedStates); Assert.Equal(0, rt.RetainedPcmAndScratchBytes);
        Assert.Equal(rt.PreparedStatesCreated, rt.PreparedStatesReleased);
    }

    [Fact]
    public async Task MaximumPatternSongAndR8UseSameMusicalRendererAndFrozenPcm16()
    {
        using var f = await Fixture.Create(GraphPreparation.MaximumVoices);
        f.Document.Edit("Song and Mixer", e => { e.AddArrangementClip(f.Placement, default, 0); e.InitializeMixer(); });
        using var pattern = f.Prepare(packet: 1024);
        using var song = ArrangementPreparation.Prepare(f.Document, 48000, 2, 1024, TestContext.Current.CancellationToken);
        Assert.Equal(Render(pattern, 127), Render(song, 511));
        string patternPath = f.Directory.File("pattern.wav"), songPath = f.Directory.File("song.wav");
        await PatternWaveExport.ExportAsync(f.Document, f.Attachment.Id, patternPath, TestContext.Current.CancellationToken);
        await PatternWaveExport.ExportArrangementAsync(f.Document, songPath, TestContext.Current.CancellationToken);
        Assert.Equal(File.ReadAllBytes(patternPath), File.ReadAllBytes(songPath));
        var capture = PatternResampling.RenderAsync(f.Document, f.Attachment.Id, TestContext.Current.CancellationToken);
        using var local = GraphPreparation.PrepareObject(f.Document, f.Attachment.Id, 48000, 2, 1024,
            TestContext.Current.CancellationToken);
        f.Document.Edit("Change after capture", e => e.SetPartNotes(f.Pattern, f.Part, []));
        var sample = await capture;
        Assert.Equal(local.EndFrame, sample.Frames);
        float[] expected = Render(local, 127, smooth: true);
        using var wave = WavDecoder.Decode(sample.Wave); using var lease = wave.Lease();
        Assert.Equal(expected.Length, lease.Samples.Length);
        for (int i = 0; i < expected.Length; i++)
            Assert.InRange(Math.Abs(expected[i] - lease.Samples[i]), 0, 1d / 32768);
    }

    [Theory]
    [InlineData(44100, 1)] [InlineData(48000, 2)]
    public async Task PatternAndOverlappingAudioClipsShareOneCapacityAndIndependentPcm(int rate, int channels)
    {
        using var f = await Fixture.Create(GraphPreparation.MaximumVoices - 4, channels);
        float[] audio = [.. Enumerable.Range(0, 4096).SelectMany(i => Enumerable.Range(0, channels)
            .Select(c => .001f * (c == 0 ? 1 : -.5f)))];
        string path = f.Directory.File("audio.wav"), normalized = f.Directory.File("normalized.wav");
        File.WriteAllBytes(path, WavFixtures.Create(audio, 48000, channels));
        AudioFileDecode.Normalize(path, normalized, TestContext.Current.CancellationToken);
        using var import = await AudioMedia.PrepareImportAsync(f.Document, normalized, "Audio", "Authored fixture",
            f.Directory.File("audio-owned"), TestContext.Current.CancellationToken);
        var clip = import.Accept(default, 1, TestContext.Current.CancellationToken);
        f.Document.Edit("Shared capacity", e =>
        {
            e.AddArrangementClip(f.Placement, default, 0);
            for (int i = 0; i < 3; i++) e.CopyAudioClip(clip, default, i + 2);
            if (channels == 2)
            {
                e.InitializeMixer();
                e.SetMixerStrip(null, new()
                {
                    Eq = new() { Enabled = true, HighPassHz = 40, MidDb = -2 },
                    Compressor = new() { Enabled = true },
                    Delay = new() { Enabled = true, TimeMs = 20, Feedback = .1m, Mix = .1m },
                    Reverb = new() { Enabled = true, DecaySeconds = .2m, Mix = .1m }
                });
            }
        });
        using var plan = ArrangementPreparation.Prepare(f.Document, rate, channels, 511, TestContext.Current.CancellationToken);
        float[] expected = Render(plan, 1); Assert.Equal(expected, Render(plan, 511));
        using var rt = new RealtimeSampler(rate, channels, 511);
        using var request = rt.BeginArrangementPreparation(f.Document);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken)); rt.Start();
        var actual = new float[expected.Length];
        for (int at = 0; at < actual.Length;)
        {
            int size = Math.Min(127 * channels, actual.Length - at); rt.Process(actual.AsSpan(at, size)); at += size;
        }
        Assert.Equal(expected, actual); Assert.Equal(GraphPreparation.MaximumVoices, rt.PeakVoices);
        f.Document.Edit("One clip too many", e => e.CopyAudioClip(clip, default, 5));
        Assert.Throws<GraphPreparationException>(() => ArrangementPreparation.Prepare(f.Document, rate, channels, 511,
            TestContext.Current.CancellationToken));
    }
}
