// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class ManagedMediaTests
{
    [Fact]
    public async Task UnnamedAcceptanceSurvivesSourceRemovalSaveAsReopenAndRelocation()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        string sourcePath = directory.File("external.wav");
        await File.WriteAllBytesAsync(sourcePath, WavFixtures.Create([0.5f, -0.25f]), TestContext.Current.CancellationToken);
        var before = document.Current;
        using var import = ProjectMedia.BeginImport(document, "Sample", 60.5m, 5m, ownedMediaDirectory: directory.File("owned"));
        await import.PrepareAsync(sourcePath, TestContext.Current.CancellationToken);
        Assert.Same(before, document.Current); Assert.Empty(document.Current.State.Resources);
        Assert.Null(document.SavedPath); Assert.Single(Directory.GetFiles(directory.File("owned"), "*.wav", SearchOption.AllDirectories));
        var accepted = import.Accept(TestContext.Current.CancellationToken);
        Assert.True(document.IsDirty); Assert.Equal(1, document.UndoCount);
        File.Delete(sourcePath);
        using (var decoded = ProjectMedia.Decode(document, accepted.ResourceId)) Assert.Equal(0.5f, decoded.Sample(0, 0));
        var identity = document.Current.State.Id;
        string first = directory.File("first.json"), second = directory.File("second.json");
        ProjectPersistence.Save(document, first);
        document.Edit("Unsaved work", edit => edit.RenameDocument("Named"));
        ProjectPersistence.Save(document, second);
        Assert.Equal(identity, document.Current.State.Id); Assert.False(document.IsDirty);
        document.Close();
        var reopened = ProjectPersistence.Open(second);
        Assert.False(reopened.IsDegraded); Assert.Equal("Named", reopened.Document.Current.State.Name);
        Assert.Equal(accepted.SoundId, reopened.Document.Current.State.Sounds.Single().Id);
        using (var decoded = ProjectMedia.Decode(reopened.Document, accepted.ResourceId)) Assert.Equal(-0.25f, decoded.Sample(1, 0));
        var relocated = directory.File("relocated"); Directory.CreateDirectory(relocated);
        File.Copy(second, Path.Combine(relocated, "second.json"));
        var locator = reopened.Document.Current.State.Resources.Single().ManagedLocator!;
        Directory.CreateDirectory(Path.Combine(relocated, "second.json.media", "wav"));
        File.Copy(Path.Combine(second + ".media", locator.Replace('/', Path.DirectorySeparatorChar)),
            Path.Combine(relocated, "second.json.media", locator.Replace('/', Path.DirectorySeparatorChar)));
        Assert.False(ProjectPersistence.Open(Path.Combine(relocated, "second.json")).IsDegraded);
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("corrupt")]
    [InlineData("missing")]
    public async Task DecodeUsesFirstValidRetainedCopyAndOwnsReturnedPcm(string newestCopy)
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        var accepted = await WavFixtures.Import(document, directory, [0.5f, -0.25f]);
        string owned = Directory.GetFiles(directory.File("owned"), "*.wav", SearchOption.AllDirectories).Single();
        string first = directory.File("first.json");
        ProjectPersistence.Save(document, first);
        var locator = document.Current.State.Resources.Single().ManagedLocator!;
        string newest = Path.Combine(first + ".media", locator.Replace('/', Path.DirectorySeparatorChar));
        if (newestCopy == "valid") File.WriteAllBytes(owned, [1, 2, 3]);
        if (newestCopy == "corrupt") File.WriteAllBytes(newest, WavFixtures.Create([0.75f, 0.25f]));
        if (newestCopy == "missing") File.Delete(newest);
        byte[] ownedBytes = File.ReadAllBytes(owned);
        byte[]? newestBytes = File.Exists(newest) ? File.ReadAllBytes(newest) : null;

        using var pcm = ProjectMedia.Decode(document, accepted.ResourceId);
        Assert.Equal(48000, pcm.SampleRate);
        Assert.Equal(1, pcm.Channels);
        Assert.Equal(2, pcm.Frames);
        Assert.Equal(0.5f, pcm.Sample(0, 0));
        Assert.Equal(-0.25f, pcm.Sample(1, 0));
        using (var independent = ProjectMedia.Decode(document, accepted.ResourceId))
            Assert.Equal(0.5f, independent.Sample(0, 0));

        string second = directory.File("second.json");
        Assert.False(ProjectPersistence.SaveWithReport(document, second).IsDegraded);
        var reopened = ProjectPersistence.Open(second);
        Assert.False(reopened.IsDegraded);
        reopened.Document.Close();
        document.Close();
        Assert.Equal(-0.25f, pcm.Sample(1, 0));
        Assert.Equal(ownedBytes, File.ReadAllBytes(owned));
        if (newestBytes is null) Assert.False(File.Exists(newest));
        else Assert.Equal(newestBytes, File.ReadAllBytes(newest));
    }

    [Theory]
    [InlineData("missing", "missing")]
    [InlineData("corrupt", "missing")]
    [InlineData("missing", "corrupt")]
    [InlineData("corrupt", "corrupt")]
    public async Task DecodeRejectsAllUnavailableRetainedCopies(string newestCopy, string olderCopy)
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        var accepted = await WavFixtures.Import(document, directory, [0.5f]);
        string owned = Directory.GetFiles(directory.File("owned"), "*.wav", SearchOption.AllDirectories).Single();
        string project = directory.File("project.json");
        ProjectPersistence.Save(document, project);
        var locator = document.Current.State.Resources.Single().ManagedLocator!;
        string newest = Path.Combine(project + ".media", locator.Replace('/', Path.DirectorySeparatorChar));
        if (newestCopy == "missing") File.Delete(newest);
        else File.WriteAllBytes(newest, [1, 2, 3]);
        if (olderCopy == "missing") File.Delete(owned);
        else File.WriteAllBytes(owned, [1, 2, 3]);

        if (newestCopy == "missing" && olderCopy == "missing")
        {
            var error = Assert.Throws<FileNotFoundException>(() => ProjectMedia.Decode(document, accepted.ResourceId));
            Assert.Equal($"Managed WAV {accepted.ResourceId} is missing.", error.Message);
        }
        else
        {
            var error = Assert.Throws<IOException>(() => ProjectMedia.Decode(document, accepted.ResourceId));
            Assert.Equal($"Managed WAV {accepted.ResourceId} has no usable stored copy.", error.Message);
            Assert.IsType<WavFormatException>(error.InnerException);
        }
    }

    [Fact]
    public async Task ReplacingExplicitSoundIsOneEditAndRetainsOldMediaAcrossUndoRedoSaveAndLiveDecode()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        var first = await WavFixtures.Import(document, directory, [0.25f]);
        string path = directory.File("project.json"); ProjectPersistence.Save(document, path);
        var saved = document.Saved;
        using var oldPcm = ProjectMedia.Decode(document, first.ResourceId);
        using var import = ProjectMedia.BeginImport(document, "Replacement", target: first.SoundId, rootPitch: 61m, releaseMilliseconds: 0);
        using var source = new MemoryStream(WavFixtures.Create([0.75f]));
        await import.PrepareAsync(source, TestContext.Current.CancellationToken);
        var replacement = import.Accept(TestContext.Current.CancellationToken);
        Assert.Equal(first.SoundId, replacement.SoundId); Assert.Equal(2, document.UndoCount);
        Assert.Equal(replacement.ResourceId, document.Current.State.Sounds.Single().ResourceIds.Single());
        document.Undo(); Assert.Same(saved, document.Current); Assert.False(document.IsDirty);
        document.Redo();
        ProjectPersistence.Save(document, path);
        document.Undo();
        using (var decoded = ProjectMedia.Decode(document, first.ResourceId)) Assert.Equal(0.25f, decoded.Sample(0, 0));
        Assert.Contains(document.RetainedSnapshots, snapshot => snapshot.State.Resources.Any(item => item.Id == replacement.ResourceId));
        document.Close(); Assert.Equal(0.25f, oldPcm.Sample(0, 0));
        Assert.False(ProjectPersistence.Open(path).IsDegraded);
    }

    [Theory]
    [InlineData("edit")]
    [InlineData("undo-redo")]
    [InlineData("delete-target")]
    [InlineData("close")]
    [InlineData("cancel")]
    [InlineData("corrupt-prepared")]
    public async Task StaleCancelledOrDamagedCandidatesNeverPublishOrAttachElsewhere(string failure)
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        var first = await WavFixtures.Import(document, directory, [0.25f]);
        using var import = ProjectMedia.BeginImport(document, "Late candidate", target: first.SoundId);
        using var stream = new GateStream(WavFixtures.Create([0.75f]));
        var preparation = import.PrepareAsync(stream, TestContext.Current.CancellationToken);
        await stream.Entered.Task;
        if (failure == "edit") document.Edit("Changed dependency", edit => edit.SetSoundParameters(first.SoundId,
            document.Current.State.Sounds.Single().Parameters.SetItem("rootPitch", 62)));
        if (failure == "undo-redo") { document.Undo(); document.Redo(); }
        if (failure == "delete-target") document.Edit("Delete intended target", edit => edit.DeleteSound(first.SoundId));
        if (failure == "close") document.Close();
        stream.Continue.TrySetResult(); await preparation;
        var before = document.Current; var generation = document.Generation; var saved = document.Saved;
        int history = document.UndoCount;
        using var cancellation = new CancellationTokenSource();
        if (failure == "cancel") cancellation.Cancel();
        if (failure == "corrupt-prepared")
        {
            var candidate = Directory.GetFiles(directory.File("owned"), "*.wav", SearchOption.AllDirectories)
                .Single(path => File.ReadAllBytes(path).SequenceEqual(WavFixtures.Create([0.75f])));
            await File.WriteAllBytesAsync(candidate, WavFixtures.Create([0.5f]), TestContext.Current.CancellationToken);
        }
        Assert.NotNull(Record.Exception(() => import.Accept(cancellation.Token)));
        Assert.Same(before, document.Current); Assert.Same(saved, document.Saved);
        Assert.Equal(generation, document.Generation); Assert.Equal(history, document.UndoCount);
        var otherDocument = ProjectDocument.Create(); Assert.Empty(otherDocument.Current.State.Resources);
        import.Dispose();
        Assert.Single(Directory.GetFiles(directory.File("owned"), "*.wav", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData("cancel")]
    [InlineData("interrupted")]
    [InlineData("malformed")]
    [InlineData("storage")]
    public async Task PreparationFailureLeavesDocumentAndOwnedStorageCoherent(string failure)
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        document.Edit("Existing work", edit => edit.RenameDocument("Keep"));
        var before = document.Current;
        string owned = directory.File("owned");
        if (failure == "storage") await File.WriteAllTextAsync(owned, "Filesystem blocker", TestContext.Current.CancellationToken);
        using var import = ProjectMedia.BeginImport(document, "Rejected", ownedMediaDirectory: owned);
        if (failure is "cancel" or "interrupted")
        {
            using var stream = new GateStream(WavFixtures.Create([0.5f]), fail: failure == "interrupted");
            using var cancellation = new CancellationTokenSource();
            var preparation = import.PrepareAsync(stream, cancellation.Token);
            await stream.Entered.Task;
            if (failure == "cancel") cancellation.Cancel();
            stream.Continue.TrySetResult();
            await Assert.ThrowsAnyAsync<Exception>(() => preparation);
        }
        else
        {
            using var stream = new MemoryStream(failure == "malformed" ? [1, 2, 3] : WavFixtures.Create([0.5f]));
            await Assert.ThrowsAnyAsync<IOException>(() => import.PrepareAsync(stream, TestContext.Current.CancellationToken));
        }
        Assert.Same(before, document.Current); Assert.Equal(1, document.UndoCount); Assert.Null(document.Saved);
        Assert.Throws<InvalidOperationException>(() => import.Accept(TestContext.Current.CancellationToken));
        if (Directory.Exists(owned)) Assert.Empty(Directory.GetFiles(owned, "*", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("corrupt")]
    [InlineData("valid-but-changed")]
    public async Task ReopeningMissingOrDamagedMediaRetainsStateAndReportsActualAvailability(string failure)
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        var accepted = await WavFixtures.Import(document, directory, [0.5f]);
        string project = directory.File("project.json"); ProjectPersistence.Save(document, project);
        var encoded = ProjectPersistence.Encode(document);
        var resource = document.Current.State.Resources.Single();
        string path = Path.Combine(project + ".media", resource.ManagedLocator!.Replace('/', Path.DirectorySeparatorChar));
        if (failure == "missing") File.Delete(path);
        else await File.WriteAllBytesAsync(path, failure == "corrupt" ? [1, 2, 3] : WavFixtures.Create([0.75f]), TestContext.Current.CancellationToken);
        document.Close();
        var reopened = ProjectPersistence.Open(project, availableResources: new HashSet<Id<ResourceDescriptor>> { accepted.ResourceId });
        Assert.True(reopened.IsDegraded); Assert.Single(reopened.Diagnostics);
        Assert.Equal(encoded, ProjectPersistence.Encode(reopened.Document));
        Assert.ThrowsAny<IOException>(() => ProjectMedia.Decode(reopened.Document, accepted.ResourceId));
        reopened.Document.Edit("Unrelated safe edit", edit => edit.RenameDocument("Still editable"));
        var degradedSave = ProjectPersistence.SaveWithReport(reopened.Document, project);
        Assert.True(degradedSave.IsDegraded); Assert.Single(reopened.Document.SavedMediaDiagnostics);
        Assert.False(reopened.Document.IsDirty);
        var degradedReopen = ProjectPersistence.Open(project);
        Assert.True(degradedReopen.IsDegraded); Assert.Equal("Still editable", degradedReopen.Document.Current.State.Name);
        var copy = ProjectPersistence.SaveWithReport(degradedReopen.Document, directory.File("degraded-copy.json"));
        Assert.True(copy.IsDegraded); Assert.True(ProjectPersistence.Open(copy.Path).IsDegraded);
        Assert.Equal(resource, degradedReopen.Document.Current.State.Resources.Single());
    }

    [Theory]
    [InlineData("media-directory")]
    [InlineData("conflicting-media")]
    [InlineData("json-destination")]
    public async Task FailedSaveAsPreservesPreviousProjectAndUnsavedWork(string failure)
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        await WavFixtures.Import(document, directory, [0.5f]);
        string old = directory.File("previous.json"); ProjectPersistence.Save(document, old);
        byte[] previous = File.ReadAllBytes(old); var saved = document.Saved;
        document.Edit("Unsaved work", edit => edit.RenameDocument("Keep unsaved"));
        var current = document.Current; int history = document.UndoCount; long generation = document.Generation;
        string target = directory.File("target.json");
        if (failure == "media-directory") File.WriteAllText(target + ".media", "Block directory");
        if (failure == "conflicting-media")
        {
            Directory.CreateDirectory(Path.Combine(target + ".media", "wav"));
            var locator = document.Current.State.Resources.Single().ManagedLocator!;
            File.WriteAllBytes(Path.Combine(target + ".media", locator.Replace('/', Path.DirectorySeparatorChar)), WavFixtures.Create([0.75f]));
        }
        if (failure == "json-destination") Directory.CreateDirectory(target);
        Assert.NotNull(Record.Exception(() => ProjectPersistence.Save(document, target)));
        Assert.Equal(previous, File.ReadAllBytes(old)); Assert.Same(saved, document.Saved); Assert.Equal(old, document.SavedPath);
        Assert.Same(current, document.Current); Assert.Equal(history, document.UndoCount); Assert.Equal(generation, document.Generation);
        Assert.True(document.IsDirty); Assert.False(ProjectPersistence.Open(old).IsDegraded);
        Assert.Empty(Directory.GetFiles(directory.Path, "*.pending", SearchOption.AllDirectories));
        Assert.Empty(Directory.GetFiles(directory.Path, ".seqvium-*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task PreparedFileIsNeverCanonicalAndRejectedEditCleansOnlyItsCandidate()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        var accepted = await WavFixtures.Import(document, directory, [0.25f]);
        var before = document.Current;
        using (var import = ProjectMedia.BeginImport(document, "Cancelled candidate"))
        {
            using var stream = new MemoryStream(WavFixtures.Create([0.75f]));
            await import.PrepareAsync(stream, TestContext.Current.CancellationToken);
            Assert.Same(before, document.Current);
            Assert.Equal(2, Directory.GetFiles(directory.File("owned"), "*.wav", SearchOption.AllDirectories).Length);
        }
        Assert.Same(before, document.Current);
        Assert.Single(Directory.GetFiles(directory.File("owned"), "*.wav", SearchOption.AllDirectories));
        using var pcm = ProjectMedia.Decode(document, accepted.ResourceId); Assert.Equal(0.25f, pcm.Sample(0, 0));
    }

    [Fact]
    public async Task SaveDuringPreparationCapturesOnlyAcceptedStateAndLateAcceptanceRemainsDirty()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        using var import = ProjectMedia.BeginImport(document, "Pending WAV", ownedMediaDirectory: directory.File("owned"));
        using var source = new GateStream(WavFixtures.Create([0.5f]));
        var preparation = import.PrepareAsync(source, TestContext.Current.CancellationToken);
        await source.Entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        string path = directory.File("project.json"); ProjectPersistence.Save(document, path);
        var saved = document.Saved;
        source.Continue.TrySetResult(); await preparation;
        var accepted = import.Accept(TestContext.Current.CancellationToken);
        Assert.Same(saved, document.Saved); Assert.True(document.IsDirty);
        Assert.Empty(ProjectPersistence.Open(path).Document.Current.State.Resources);
        ProjectPersistence.Save(document, path);
        var reopened = ProjectPersistence.Open(path); Assert.False(reopened.IsDegraded);
        using var pcm = ProjectMedia.Decode(reopened.Document, accepted.ResourceId); Assert.Equal(0.5f, pcm.Sample(0, 0));
    }

    [Fact]
    public async Task CancellationAfterPreparationCannotBeOverriddenByAnotherAcceptanceToken()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create(); var before = document.Current;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var import = ProjectMedia.BeginImport(document, "Withdrawn", ownedMediaDirectory: directory.File("owned"));
        using var source = new MemoryStream(WavFixtures.Create([0.5f]));
        await import.PrepareAsync(source, cancellation.Token);
        cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => import.Accept(TestContext.Current.CancellationToken));
        Assert.Throws<InvalidOperationException>(() => import.Accept(TestContext.Current.CancellationToken));
        Assert.Same(before, document.Current); Assert.Equal(0, document.UndoCount);
        import.Dispose(); Assert.Empty(Directory.GetFiles(directory.File("owned"), "*.wav", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task PartiallyTransferredSaveAsDoesNotPublishJsonOrMutateThePriorSave()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        await WavFixtures.Import(document, directory, [0.25f]);
        await WavFixtures.Import(document, directory, [0.75f]);
        string previous = directory.File("previous.json"); ProjectPersistence.Save(document, previous);
        var bytes = File.ReadAllBytes(previous); var saved = document.Saved;
        document.Edit("Current work", edit => edit.RenameDocument("Keep"));
        string destination = directory.File("destination.json");
        Directory.CreateDirectory(Path.Combine(destination + ".media", "wav"));
        var second = document.Current.State.Resources[1].ManagedLocator!;
        File.WriteAllBytes(Path.Combine(destination + ".media", second.Replace('/', Path.DirectorySeparatorChar)), [1, 2, 3]);
        Assert.ThrowsAny<IOException>(() => ProjectPersistence.Save(document, destination));
        Assert.False(File.Exists(destination)); Assert.Equal(bytes, File.ReadAllBytes(previous));
        Assert.Same(saved, document.Saved); Assert.True(document.IsDirty);
        Assert.Equal(2, Directory.GetFiles(destination + ".media", "*.wav", SearchOption.AllDirectories).Length);
        Assert.False(ProjectPersistence.Open(previous).IsDegraded);
    }

    [Fact]
    public async Task RealMediaFormatRetainsR1UnknownAndOpaqueDataAcrossImportHistoryAndSaveAs()
    {
        using var directory = new TemporaryDirectory();
        var initial = ProjectDocument.Create();
        initial.Edit("Opaque unrelated dependency", edit => edit.AddSound("Opaque", "extension",
            extension: new("example.foreign", 1, System.Text.Json.JsonSerializer.SerializeToElement(new { retained = true }))));
        var json = System.Text.Json.Nodes.JsonNode.Parse(ProjectPersistence.Encode(initial))!;
        json["minor"] = 3; json["state"]!["future"] = "preserve";
        using var input = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString()));
        var document = ProjectPersistence.Read(input, new HashSet<string> { "example.foreign" }).Document;
        await WavFixtures.Import(document, directory, [0.5f]);
        document.Undo(); document.Redo();
        string first = directory.File("first.json"), second = directory.File("second.json");
        ProjectPersistence.Save(document, first); ProjectPersistence.Save(document, second);
        var reopened = ProjectPersistence.Open(second, new HashSet<string> { "example.foreign" });
        Assert.False(reopened.IsDegraded);
        var result = System.Text.Json.Nodes.JsonNode.Parse(ProjectPersistence.Encode(reopened.Document))!;
        Assert.Equal(3, result["minor"]!.GetValue<int>()); Assert.Equal("preserve", result["state"]!["future"]!.GetValue<string>());
        Assert.True(reopened.Document.Current.State.Sounds[0].Extension!.Payload.GetProperty("retained").GetBoolean());
    }

    private sealed class GateStream(byte[] bytes, bool fail = false) : MemoryStream(bytes)
    {
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _entered;
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (Position == 0) return await base.ReadAsync(buffer[..Math.Min(8, buffer.Length)], cancellationToken);
            if (!_entered)
            {
                _entered = true; Entered.TrySetResult();
                await Continue.Task.WaitAsync(cancellationToken);
                if (fail) throw new IOException("Deterministically interrupted source read.");
            }
            return await base.ReadAsync(buffer, cancellationToken);
        }
    }
}
