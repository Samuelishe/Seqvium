// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class PianoRollTests
{
    private sealed class Borrower : IAudioOutputLifetime
    {
        public bool Joined { get; private set; }
        public void Close() => Joined = true;
        public void Dispose() => Assert.True(Joined);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly DesktopWorkflow Workflow;
        internal RealtimeSampler? Sampler;
        internal int Opens;

        internal Fixture() => Workflow = new(ProjectDocument.Create(), () =>
        {
            Opens++;
            Sampler = new(48000, 2, 64);
            return new(new Borrower(), Sampler, "Controlled output", () => null);
        }, Directory.File("owned"));

        internal WorkflowState State => Workflow.State;
        internal Pattern Pattern => BeatSteps.Resolve(State).Pattern!;
        internal MusicalPart Part => Pattern.Parts[0];
        internal BeatEditBasis Basis => BeatSteps.Resolve(State).Basis!;

        internal async Task Create()
        {
            Assert.True(await Workflow.CreateBeatAsync(State.Document.LifecycleId, State.Generation, "Phrase"));
            string path = Directory.File("tone.wav");
            await File.WriteAllBytesAsync(path, WavFixtures.Create(Enumerable.Repeat(.25f, 48000).ToArray()),
                TestContext.Current.CancellationToken);
            Assert.True(await Workflow.AddBeatWavAsync(Basis, path));
        }

        internal Task<bool> External(Action<ProjectEdit> edit) => Workflow.EditGraphAsync(
            new(State.Document.LifecycleId, State.Generation, State.Selected!.Value), "External edit",
            (e, _, _) => edit(e));

        public async ValueTask DisposeAsync()
        {
            await Workflow.DisposeAsync();
            Directory.Dispose();
        }
    }

    private static NoteEvent Note(long at, long length, decimal pitch, decimal velocity = .8m) =>
        new(Id<NoteEvent>.New(), new(at), new(length), pitch, velocity);

    [Fact]
    public async Task ChordAndMelodyShareCanonicalHistorySaveAndExportWithoutRackFlattening()
    {
        await using var f = new Fixture();
        await f.Create();
        NoteEvent[] notes =
            [Note(0, 960000, 60, .35m), Note(0, 1920000, 64), Note(0, 960000, 67), Note(1234567, 123457, 81, .65m)];
        var before = f.State.Snapshot;
        int undo = f.State.UndoCount;
        Assert.True(await f.Workflow.EditPianoNotesAsync(f.Basis, f.Part.Id, notes));
        var authored = f.State.Snapshot;
        Assert.Equal(undo + 1, f.State.UndoCount);
        Assert.Equal(notes, f.Part.Notes);
        Assert.Equal("Beat.NotesUnsupported", BeatSteps.PartRefusal(authored.State, f.Part, f.Pattern.Length.Ticks));
        Assert.False(await f.Workflow.SetBeatStepsAsync(f.Basis, f.Part.Id, Enumerable.Repeat(false, 16).ToArray()));
        Assert.Same(authored, f.State.Snapshot);
        Assert.False(await f.Workflow.EditPianoNotesAsync(f.Basis, f.Part.Id, notes));
        Assert.False(await f.Workflow.EditPianoNotesAsync(f.Basis, f.Part.Id, notes.Reverse().ToArray()));
        Assert.Equal(undo + 1, f.State.UndoCount);
        Assert.True(await f.Workflow.UndoAsync());
        Assert.Same(before, f.State.Snapshot);
        Assert.True(await f.Workflow.UndoAsync(true));
        Assert.Same(authored, f.State.Snapshot);
        Assert.True(await f.External(e => e.AddPlacement(f.Pattern.Id, new(3840000))));
        var edited = notes.Select(n => n with { Intensity = n.Intensity / 2 }).ToArray();
        Assert.True(await f.Workflow.EditPianoNotesAsync(f.Basis, f.Part.Id, edited));
        Assert.Equal(2, f.State.Snapshot.State.Placements.Count(p => p.PatternId == f.Pattern.Id));
        string save = f.Directory.File("phrase.seqvium");
        Assert.True(await f.Workflow.SaveAsync(save));
        var saved = f.State.Snapshot;
        Assert.True(await f.Workflow.OpenAsync(save, UnsavedDecision.Discard));
        Assert.Equal(edited, f.Part.Notes);
        Assert.Equal(saved.Revision, f.State.Snapshot.Revision);
        Assert.Equal(0, f.Opens);
        Assert.True(
            await f.Workflow.ExportWaveAsync(f.Basis, f.Directory.File("phrase.wav"),
                TestContext.Current.CancellationToken), f.State.Detail);
        Assert.Equal(edited, f.Part.Notes);
        Assert.Equal(0, f.State.UndoCount);
        Assert.Equal(0, f.Opens);
    }

    [Fact]
    public async Task GestureAuthorityExpiresOnUndoAndAtomicValidationPreservesOtherParts()
    {
        await using var f = new Fixture();
        await f.Create();
        var basis = f.Basis;
        var note = Note(0, 960000, 64);
        Assert.True(await f.Workflow.EditPianoNotesAsync(basis, f.Part.Id, [note]));
        Assert.True(await f.Workflow.UndoAsync());
        var state = f.State.Snapshot;
        Assert.False(await f.Workflow.EditPianoNotesAsync(basis, f.Part.Id, [note]));
        Assert.Same(state, f.State.Snapshot);
        Assert.False(await f.Workflow.EditPianoNotesAsync(f.Basis, f.Part.Id, [Note(3800000, 960000, 60)]));
        Assert.Same(state, f.State.Snapshot);
        Assert.False(await f.Workflow.EditPianoNotesAsync(f.Basis, f.Part.Id, [note, note]));
        Assert.Same(state, f.State.Snapshot);
        var unknown = note with
        {
            AdditionalData = new Dictionary<string, JsonElement>
                { ["future"] = JsonSerializer.SerializeToElement(true) }.ToImmutableDictionary()
        };
        Assert.False(await f.Workflow.EditPianoNotesAsync(f.Basis, f.Part.Id, [unknown]));
        Assert.Same(state, f.State.Snapshot);
    }

    [Fact]
    public async Task PatternExtensionPreservesRichNotesAndOccupiedShorteningRefuses()
    {
        await using var f = new Fixture();
        await f.Create();
        var note = Note(3000000, 800000, 62.5m, .43m);
        Assert.True(await f.Workflow.EditPianoNotesAsync(f.Basis, f.Part.Id, [note]));
        Assert.True(await f.Workflow.ResizeBeatAsync(f.Basis, 2));
        Assert.Equal(note, f.Part.Notes.Single());
        var tail = Note(3700000, 500000, 67);
        Assert.True(await f.Workflow.EditPianoNotesAsync(f.Basis, f.Part.Id, [note, tail]));
        var before = f.State.Snapshot;
        Assert.False(await f.Workflow.ResizeBeatAsync(f.Basis, 1));
        Assert.Same(before, f.State.Snapshot);
    }

    [Fact]
    public async Task RootPitchChangesSharedSoundWithoutRewritingNotesAndSurvivesReopen()
    {
        await using var f = new Fixture();
        await f.Create();
        var note = Note(0, 960000, 60);
        Assert.True(await f.Workflow.EditPianoNotesAsync(f.Basis, f.Part.Id, [note]));
        var before = f.State.Snapshot;
        Assert.True(await f.Workflow.SetRootPitchAsync(f.Basis, f.Part.Id, 57));
        Assert.Equal(note, f.Part.Notes.Single());
        Assert.Equal(57, f.State.Snapshot.State.Sounds.Single().Parameters["rootPitch"]);
        Assert.True(await f.Workflow.UndoAsync());
        Assert.Same(before, f.State.Snapshot);
        Assert.True(await f.Workflow.UndoAsync(true));
        string save = f.Directory.File("root.seqvium");
        Assert.True(await f.Workflow.SaveAsync(save));
        Assert.True(await f.Workflow.OpenAsync(save, UnsavedDecision.Discard));
        Assert.Equal(57, f.State.Snapshot.State.Sounds.Single().Parameters["rootPitch"]);
        Assert.Equal(note, f.Part.Notes.Single());
    }

    [Fact]
    public async Task ChannelNameAndFractionalRootAreOneAtomicEditWithoutNoteRewrite()
    {
        await using var f = new Fixture();
        await f.Create();
        var note = Note(0, 960000, 60, .5m);
        Assert.True(await f.Workflow.EditPianoNotesAsync(f.Basis, f.Part.Id, [note]));
        var before = f.State.Snapshot;
        int undo = f.State.UndoCount;
        Assert.True(await f.Workflow.ConfigurePianoSourceAsync(f.Basis, f.Part.Id, "Lead piano", 57.5m));
        Assert.Equal(undo + 1, f.State.UndoCount);
        Assert.Equal("Lead piano", f.Part.Name);
        Assert.Equal(57.5m, f.State.Snapshot.State.Sounds.Single().Parameters["rootPitch"]);
        Assert.Equal(note, f.Part.Notes.Single());
        Assert.True(await f.Workflow.UndoAsync());
        Assert.Same(before, f.State.Snapshot);
        Assert.False(await f.Workflow.ConfigurePianoSourceAsync(f.Basis, f.Part.Id, "Changed", 128));
        Assert.Same(before, f.State.Snapshot);
        Assert.False(await f.Workflow.ConfigurePianoSourceAsync(f.Basis, f.Part.Id, "", 57));
        Assert.Same(before, f.State.Snapshot);
    }

    [Theory]
    [InlineData(0, -1000000, -100, 0, 0)]
    [InlineData(3000000, 1000000, 100, 2880000, 127)]
    public void GroupMotionClampsTogetherAndCopyAcquiresNewIds(long start, long delta, decimal pitchDelta,
        long expected, decimal expectedPitch)
    {
        var note = Note(start, 960000, 60);
        var moved = PianoNotes.Move([note], delta, pitchDelta, 3840000, true).Single();
        Assert.Equal(expected, moved.Position.Ticks);
        Assert.Equal(expectedPitch, moved.Pitch);
        Assert.NotEqual(note.Id, moved.Id);
        Assert.Equal(note.Duration, moved.Duration);
        Assert.Equal(note.Intensity, moved.Intensity);
    }

    [Theory]
    [InlineData("valid", 57)]
    [InlineData("hash", 60)]
    [InlineData("path", 60)]
    [InlineData("null", 60)]
    [InlineData("broken", 60)]
    public async Task CatalogRootRequiresExactPathAndByteHash(string kind, decimal expected)
    {
        using var directory = new TemporaryDirectory();
        byte[] bytes = WavFixtures.Create([.1f, .2f]);
        string path = directory.File("Ready/tone.wav");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string json = kind == "broken"
            ? "invalid"
            : JsonSerializer.Serialize(new[]
            {
                new
                {
                    file = kind == "path" ? "Ready/other.wav" : "Ready/tone.wav",
                    root_midi = kind == "null" ? (decimal?)null : 57,
                    sha256 = kind == "hash" ? "incorrect" : Convert.ToHexStringLower(SHA256.HashData(bytes))
                }
            });
        await File.WriteAllTextAsync(directory.File("catalog.json"), json, TestContext.Current.CancellationToken);
        Assert.Equal(expected, SampleRootMetadata.Read(path, bytes));
    }

    [Fact]
    public async Task PitchedAuditionIsTransientAndOutputJoinsBeforePatternPlayback()
    {
        await using var f = new Fixture();
        await f.Create();
        var before = f.State.Snapshot;
        Assert.True(await f.Workflow.AuditionNoteAsync(f.Basis, f.Part.Id, 84, .5m));
        Assert.Same(before, f.State.Snapshot);
        var audition = f.Sampler!;
        float[] packet = new float[128];
        audition.Process(packet);
        Assert.Contains(packet, value => value > 0);
        Assert.True(audition.ReadStatus().Playing);
        Assert.True(await f.Workflow.StopAsync());
        audition.Process(packet);
        Assert.All(packet, value => Assert.Equal(0, value));
        Assert.Same(before, f.State.Snapshot);
        var play = f.Workflow.PlayAsync();
        while (!play.IsCompleted)
        {
            f.Sampler?.Process(Span<float>.Empty);
            await f.Workflow.ObserveAsync();
            await Task.Yield();
        }

        Assert.True(await play.WaitAsync(TestContext.Current.CancellationToken), f.State.Detail);
        Assert.True(audition.IsTerminated);
        Assert.Equal(2, f.Opens);
        Assert.Same(before, f.State.Snapshot);
    }
}
