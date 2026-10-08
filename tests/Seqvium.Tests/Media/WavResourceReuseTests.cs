// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class WavResourceReuseTests
{
    [Fact]
    public async Task ExplicitImportThenReuseKeepsOnePhysicalResourceAndIndependentSoundUsesAcrossHistoryAndSave()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        string external = directory.File("source.wav");
        File.WriteAllBytes(external, WavFixtures.Create([0.2f, 0.4f]));
        using var import = ProjectMedia.BeginImport(document, "Original", rootPitch: 60, releaseMilliseconds: 0,
            ownedMediaDirectory: directory.File("owned"));
        await import.PrepareAsync(external, TestContext.Current.CancellationToken);
        Assert.Empty(document.Current.State.Resources);
        var first = import.Accept(TestContext.Current.CancellationToken);
        File.Delete(external);
        var originalSound = document.Current.State.Sounds.Single();
        var beforeReuse = document.Current;
        string owned = Directory.GetFiles(directory.File("owned"), "*.wav", SearchOption.AllDirectories).Single();
        byte[] ownedBytes = File.ReadAllBytes(owned);
        using var reuse = ProjectMedia.BeginReuse(document, first.ResourceId, "Independent", 72, 25);
        await reuse.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Same(beforeReuse, document.Current);
        var second = reuse.Accept(TestContext.Current.CancellationToken);
        Assert.Equal(first.ResourceId, second.ResourceId);
        Assert.NotEqual(first.SoundId, second.SoundId);
        Assert.Equal(originalSound, document.Current.State.Sounds[0]);
        Assert.Single(document.Current.State.Resources);
        Assert.Single(Directory.GetFiles(directory.File("owned"), "*.wav", SearchOption.AllDirectories));
        Assert.Equal(ownedBytes, File.ReadAllBytes(owned));
        Assert.Equal(2, document.UndoCount);
        Assert.True(document.Undo());
        Assert.Same(beforeReuse, document.Current);
        Assert.True(document.Redo());
        Id<Pattern> pattern = default;
        Id<MusicalPart> a = default, b = default;
        document.Edit("Two uses of accepted WAV", edit =>
        {
            pattern = edit.AddPattern("Shared audio, independent sounds", new(MusicalPosition.TicksPerQuarter));
            a = edit.AddPart(pattern, "Original", first.SoundId);
            b = edit.AddPart(pattern, "Independent", second.SoundId);
            edit.AddNote(pattern, a, default, new(MusicalPosition.TicksPerQuarter), 60, 0.5m);
            edit.AddNote(pattern, b, default, new(MusicalPosition.TicksPerQuarter), 60, 0.5m);
            edit.AddPlacement(pattern, default);
            edit.AddPlacement(pattern, new(MusicalPosition.TicksPerQuarter));
        });
        using var plan = SamplerPreparation.PreparePattern(document, pattern, 48000, voiceCapacity: 2);
        using var execution = plan.CreateExecution();
        float[] output = new float[4];
        execution.Process(output);
        Assert.InRange(Math.Abs(output[2] - 0.35f), 0, 2e-6); // original 0.4*.5 + independent interpolated 0.3*.5
        string firstSave = directory.File("first.json"), secondSave = directory.File("second.json");
        ProjectPersistence.Save(document, firstSave);
        ProjectPersistence.Save(document, secondSave);
        Assert.Single(Directory.GetFiles(secondSave + ".media", "*.wav", SearchOption.AllDirectories));
        var reopened = ProjectPersistence.Open(secondSave);
        Assert.False(reopened.IsDegraded);
        Assert.Equal(second.SoundId, reopened.Document.Current.State.Patterns.Single().Parts[1].SoundId);
        Assert.All(reopened.Document.Current.State.Sounds,
            sound => Assert.Equal(first.ResourceId, Assert.Single(sound.ResourceIds)));
        Assert.Equal(2, reopened.Document.Current.State.Placements.Length);
        using var pcm = ProjectMedia.Decode(reopened.Document, first.ResourceId);
        Assert.Equal(0.4f, pcm.Sample(1, 0));
    }

    [Theory]
    [InlineData("edit")]
    [InlineData("undo-redo")]
    [InlineData("close")]
    [InlineData("cancel")]
    [InlineData("corrupt")]
    public async Task ReuseRejectsLostAuthorityCancellationOrIntegrityBeforeAnyEdit(string failure)
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        var first = await WavFixtures.Import(document, directory, [0.5f]);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var reuse = ProjectMedia.BeginReuse(document, first.ResourceId, "Independent");
        await reuse.PrepareAsync(cancellation.Token);
        if (failure == "edit") document.Edit("Newer", edit => edit.RenameDocument("Newer"));
        if (failure == "undo-redo")
        {
            document.Undo();
            document.Redo();
        }

        if (failure == "close") document.Close();
        if (failure == "cancel") cancellation.Cancel();
        if (failure == "corrupt")
            File.WriteAllBytes(
                Directory.GetFiles(directory.File("owned"), "*.wav", SearchOption.AllDirectories).Single(), [1, 2, 3]);
        var before = document.Current;
        int undo = document.UndoCount;
        if (failure == "cancel")
            Assert.ThrowsAny<OperationCanceledException>(() => reuse.Accept(TestContext.Current.CancellationToken));
        else if (failure == "corrupt")
            Assert.ThrowsAny<IOException>(() => reuse.Accept(TestContext.Current.CancellationToken));
        else Assert.Throws<InvalidOperationException>(() => reuse.Accept(TestContext.Current.CancellationToken));
        Assert.Same(before, document.Current);
        Assert.Equal(undo, document.UndoCount);
        Assert.Single(document.Current.State.Sounds);
    }

    [Fact]
    public async Task ReuseRequiresCurrentProjectMembershipAndHealthyManagedMedia()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        var first = await WavFixtures.Import(document, directory, [0.5f]);
        Assert.Throws<InvalidOperationException>(() =>
            ProjectMedia.BeginReuse(ProjectDocument.Create(), first.ResourceId, "Foreign"));
        File.Delete(Directory.GetFiles(directory.File("owned"), "*.wav", SearchOption.AllDirectories).Single());
        using var reuse = ProjectMedia.BeginReuse(document, first.ResourceId, "Unavailable");
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            reuse.PrepareAsync(TestContext.Current.CancellationToken));
        Assert.Single(document.Current.State.Resources);
        Assert.Single(document.Current.State.Sounds);
    }
}
