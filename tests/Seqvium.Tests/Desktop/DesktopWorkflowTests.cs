// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Presentation;
using Xunit;

namespace Seqvium.Tests;

public sealed class DesktopWorkflowTests
{
    // Only the physical borrower is substituted. Document, durable import, preparation, DSP,
    // coordinator publication/identity and release are production implementations.
    private sealed class Borrower : IAudioOutputLifetime
    {
        public bool Joined { get; private set; }
        public bool FailJoin;
        public Action BeforeJoin = () => { };
        public void Close()
        {
            BeforeJoin();
            if (FailJoin) throw new IOException("Injected join failure");
            Joined = true;
        }
        public void Dispose() => Assert.True(Joined);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly ProjectDocument Document;
        internal readonly DesktopWorkflow Workflow;
        internal RealtimeSampler? Sampler;
        internal Borrower? Output;
        internal string? Fault;
        internal int Opens;
        internal Fixture(ProjectDocument? document = null,
            Func<CancellationToken, Task>? gate = null)
        {
            Document = document ?? ProjectDocument.Create();
            Workflow = new(Document, () =>
            {
                Opens++; Sampler = new(48000, 2, 32); Output = new();
                return new(Output, Sampler, "Deterministic borrower", () => Fault);
            }, Directory.File("owned"), gate is null ? null :
                (doc, sampler, target, owner) => new(doc, sampler, target, owner, 8, 1, gate, true));
        }
        internal async Task<string> Source(float[]? samples = null, int rate = 48000, bool floating = true)
        {
            var path = Directory.File(Guid.NewGuid() + ".wav");
            await File.WriteAllBytesAsync(path, WavFixtures.Create(samples ?? Enumerable.Repeat(0.5f, 48000).ToArray(),
                rate, floating: floating), TestContext.Current.CancellationToken);
            return path;
        }
        internal async Task Import() => Assert.True(await Workflow.ImportAsync(await Source()));
        internal async Task Drive(Task<bool> task)
        {
            // Explicit owner barriers and one sequential consumer, no physical device or timing sleeps.
            for (int pass = 0; !task.IsCompleted && pass < 10000; pass++)
            {
                await Workflow.ObserveAsync();
                Sampler?.Process(new float[64]);
                await Task.Yield();
            }
            Assert.True(await task.WaitAsync(TestContext.Current.CancellationToken));
        }
        public async ValueTask DisposeAsync()
        {
            if (Output is not null) Output.FailJoin = false;
            await Workflow.DisposeAsync();
            Directory.Dispose();
        }
    }

    [Theory]
    [InlineData(44100, false)]
    [InlineData(48000, true)]
    public async Task ImportCreatesOneAtomicValidUseAndExactSourceBinding(int rate, bool floating)
    {
        await using var fixture = new Fixture();
        var path = await fixture.Source([0.5f, 0.25f, -0.5f], rate, floating);
        Assert.True(await fixture.Workflow.ImportAsync(path, TestContext.Current.CancellationToken));
        var state = fixture.Document.Current.State;
        Assert.Equal(1, fixture.Document.UndoCount);
        var pattern = Assert.Single(state.Patterns);
        var part = Assert.Single(pattern.Parts);
        var note = Assert.Single(part.Notes);
        Assert.Equal(60m, note.Pitch); Assert.Equal(default, note.Position);
        Assert.Equal(pattern.Length, note.Duration);
        Assert.True(ConstantTempoConversion.ToFrames(new(pattern.Length.Ticks), state.Settings.Tempo, rate) >= 3);
        var placement = Assert.Single(state.Placements);
        var attachment = Assert.Single(state.GraphAttachments);
        var binding = Assert.Single(attachment.Sources);
        Assert.Equal(placement.Id, binding.PlacementId); Assert.Equal(part.Id, binding.PartId);
        Assert.Equal(Assert.Single(state.Sounds).Id, part.SoundId);
        Assert.Equal(3, Assert.Single(state.Graphs).Nodes.Length);
        Assert.Equal(2, Assert.Single(state.Graphs).Connections.Length);
        Assert.True(Assert.Single(GraphDiagnostics.Inspect(state)).IsEligibleForPreparation);
        File.Delete(path);
        using var decoded = ProjectMedia.Decode(fixture.Document, Assert.Single(state.Resources).Id);
        Assert.Equal(3, decoded.Frames);
        Assert.Equal(0.25m, fixture.Workflow.State.Gain);
        Assert.Equal(0, fixture.Opens);
    }

    [Fact]
    public async Task LongSourceRetainsNaturalEndBeyondOneBar()
    {
        await using var fixture = new Fixture();
        var path = await fixture.Source(Enumerable.Repeat(0.5f, 48000 * 5).ToArray());
        Assert.True(await fixture.Workflow.ImportAsync(path, TestContext.Current.CancellationToken));
        var pattern = Assert.Single(fixture.Document.Current.State.Patterns);
        Assert.True(pattern.Length.Ticks > MusicalPosition.TicksPerQuarter * 4);
        using var plan = GraphPreparation.Prepare(fixture.Document,
            fixture.Workflow.State.Selected!.Value, 48000, 2, 32, cancellationToken: TestContext.Current.CancellationToken);
        using var execution = plan.CreateExecution();
        var pcm = new float[plan.EndFrame * 2];
        for (int offset = 0; offset < pcm.Length; offset += 64)
            execution.Process(pcm.AsSpan(offset, Math.Min(64, pcm.Length - offset)));
        Assert.Equal(0.125f, pcm[(48000 * 5 - 1) * 2]);
        Assert.True(plan.EndFrame >= 48000 * 5);
    }

    [Theory]
    [InlineData(44100, 48000, 3, "120")]
    [InlineData(48000, 44100, 10, "1000")]
    [InlineData(44100, 48000, 100, "137.125")]
    public async Task ImportedDurationDoesNotCutOffResampledNaturalEof(int sourceRate, int executionRate,
        int frames, string bpm)
    {
        var tempo = new Tempo(decimal.Parse(bpm, System.Globalization.CultureInfo.InvariantCulture));
        await using var fixture = new Fixture(ProjectDocument.Create(new(tempo, new Meter(4, 4))));
        var path = await fixture.Source(Enumerable.Repeat(0.5f, frames).ToArray(), sourceRate);
        Assert.True(await fixture.Workflow.ImportAsync(path, TestContext.Current.CancellationToken));
        using var plan = GraphPreparation.Prepare(fixture.Document, fixture.Workflow.State.Selected!.Value,
            executionRate, 2, 32, cancellationToken: TestContext.Current.CancellationToken);
        using var execution = plan.CreateExecution();
        var pcm = new float[plan.EndFrame * 2];
        for (int offset = 0; offset < pcm.Length; offset += 64)
            execution.Process(pcm.AsSpan(offset, Math.Min(64, pcm.Length - offset)));
        long naturalFrames = ((long)frames * executionRate + sourceRate - 1) / sourceRate;
        Assert.True(plan.EndFrame >= naturalFrames);
        Assert.All(pcm.Take(checked(((int)naturalFrames - 1) * 2)), value => Assert.Equal(0.125f, value));
        // Independent rational oracle includes the sampler's interpolation against zero beyond EOF.
        decimal remaining = frames - (decimal)((naturalFrames - 1) * sourceRate) / executionRate;
        float final = (float)(0.125m * Math.Min(1m, remaining));
        Assert.True(final > 0);
        Assert.InRange(Math.Abs(pcm[checked(((int)naturalFrames - 1) * 2)] - final), 0, 2e-6f);
        Assert.All(pcm.Skip(checked((int)naturalFrames * 2)), value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task GainEditChangesPcmWithoutRestartingPreparedIdentityOrVoices()
    {
        await using var fixture = new Fixture(); await fixture.Import();
        await fixture.Drive(fixture.Workflow.PlayAsync());
        var sampler = fixture.Sampler!;
        var before = sampler.ReadStatus();
        var pcm = new float[64]; sampler.Process(pcm);
        Assert.All(pcm, value => Assert.Equal(0.125f, value));
        int prepared = sampler.PreparedStatesCreated;
        Assert.True(await fixture.Workflow.SetGainAsync(0.5m));
        await fixture.Workflow.ObserveAsync(); sampler.Process(pcm);
        Assert.All(pcm, value => Assert.Equal(0.25f, value));
        var after = sampler.ReadStatus();
        Assert.Equal(before.PreparedExecutionId, after.PreparedExecutionId);
        Assert.Equal(before.AttachmentId, after.AttachmentId);
        Assert.Equal(before.OriginPreparedRevision, after.OriginPreparedRevision);
        Assert.Equal(fixture.Document.Current.Revision, after.EquivalentCanonicalRevision);
        Assert.Equal(prepared, sampler.PreparedStatesCreated);
        Assert.Equal(1, after.Voices); Assert.True(after.Position > before.Position);
    }

    [Fact]
    public async Task UndoRedoKeepsAtomicCanonicalUseAndRetainedMedia()
    {
        await using var fixture = new Fixture(); await fixture.Import();
        var original = fixture.Document.Current;
        Assert.True(await fixture.Workflow.SetGainAsync(0.7m));
        Assert.True(await fixture.Workflow.UndoAsync()); Assert.Same(original, fixture.Document.Current);
        Assert.True(await fixture.Workflow.UndoAsync());
        Assert.Empty(fixture.Document.Current.State.Resources); Assert.Empty(fixture.Workflow.State.Occurrences);
        Assert.True(await fixture.Workflow.UndoAsync(true)); Assert.Same(original, fixture.Document.Current);
        using var pcm = ProjectMedia.Decode(fixture.Document, original.State.Resources.Single().Id);
        Assert.Equal(0.5f, pcm.Sample(0, 0));
        Assert.True(await fixture.Workflow.UndoAsync(true)); Assert.Equal(0.7m, fixture.Workflow.State.Gain);
    }

    [Fact]
    public async Task SaveSaveAsReopenPreservesIdsMediaAndRequiresNewExplicitPlay()
    {
        await using var fixture = new Fixture(); await fixture.Import();
        var identity = fixture.Document.Current;
        var first = fixture.Directory.File("first.seqvium"); var second = fixture.Directory.File("second.seqvium");
        Assert.True(await fixture.Workflow.SaveAsync(first));
        Assert.True(await fixture.Workflow.SaveAsync(second));
        Assert.Equal(second, fixture.Document.SavedPath); Assert.False(fixture.Document.IsDirty);
        await fixture.Drive(fixture.Workflow.PlayAsync());
        var oldSampler = fixture.Sampler!;
        Assert.True(await fixture.Workflow.OpenAsync(second, UnsavedDecision.Discard));
        Assert.True(fixture.Document.IsClosed); Assert.True(fixture.Output!.Joined);
        Assert.Equal(0, oldSampler.LivePreparedStates); Assert.Equal(0, oldSampler.ReservedPcmAndScratchBytes);
        Assert.Null(fixture.Workflow.State.Execution);
        Assert.Equal(identity.Revision, fixture.Workflow.State.Document.Current.Revision);
        Assert.Equal(identity.State.GraphAttachments.Single().Id, fixture.Workflow.State.Selected);
        using var pcm = ProjectMedia.Decode(fixture.Workflow.State.Document, identity.State.Resources.Single().Id);
        Assert.Equal(0.5f, pcm.Sample(0, 0));
        Assert.Equal(1, fixture.Opens);
        await fixture.Drive(fixture.Workflow.PlayAsync()); Assert.Equal(2, fixture.Opens);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CancelledOrInvalidImportCreatesNoHistoryOrAcceptedFile(bool cancel)
    {
        await using var fixture = new Fixture();
        var snapshot = fixture.Document.Current;
        var path = await fixture.Source();
        if (!cancel) await File.WriteAllTextAsync(path, "invalid", TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource(); if (cancel) cancellation.Cancel();
        Assert.False(await fixture.Workflow.ImportAsync(path, cancellation.Token));
        Assert.Same(snapshot, fixture.Document.Current); Assert.Equal(0, fixture.Document.UndoCount);
        Assert.Empty(fixture.Workflow.State.Occurrences);
        var owned = fixture.Directory.File("owned");
        Assert.True(!Directory.Exists(owned) || Directory.GetFiles(owned, "*.wav", SearchOption.AllDirectories).Length == 0);
    }

    [Fact]
    public async Task FailedAtomicUseCallbackLeavesNoAcceptedMediaOrPartialMusic()
    {
        using var directory = new TemporaryDirectory(); var document = ProjectDocument.Create();
        var initial = document.Current;
        using (var request = ProjectMedia.BeginImport(document, "Sample", ownedMediaDirectory: directory.File("owned")))
        {
            using var input = new MemoryStream(WavFixtures.Create([0.5f]));
            await request.PrepareAsync(input, TestContext.Current.CancellationToken);
            Assert.Throws<ProjectValidationException>(() => request.Accept((edit, media) =>
            {
                edit.AddPattern("Invalid", default);
            }, TestContext.Current.CancellationToken));
        }
        Assert.Same(initial, document.Current); Assert.Equal(0, document.UndoCount);
        Assert.Empty(Directory.GetFiles(directory.File("owned"), "*.wav", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(0)]
    public async Task CancelAndFailedSavePreserveDirtyDocumentBeforeReplacement(int decisionValue)
    {
        await using var fixture = new Fixture(); await fixture.Import();
        var snapshot = fixture.Document.Current;
        var incoming = fixture.Directory.File("incoming.seqvium");
        ProjectPersistence.Save(ProjectDocument.Create(), incoming);
        var blocked = fixture.Directory.File("directory-target"); Directory.CreateDirectory(blocked);
        Assert.False(await fixture.Workflow.OpenAsync(incoming, (UnsavedDecision)decisionValue, blocked));
        Assert.Same(fixture.Document, fixture.Workflow.State.Document);
        Assert.Same(snapshot, fixture.Document.Current); Assert.True(fixture.Document.IsDirty);
        Assert.False(fixture.Document.IsClosed); Assert.Null(fixture.Document.SavedPath);
        if ((UnsavedDecision)decisionValue == UnsavedDecision.Save)
            Assert.Equal("Workflow.SaveFailed", fixture.Workflow.State.StatusKey);
    }

    [Fact]
    public async Task SaveDecisionPublishesOldWorkBeforeOpeningCandidate()
    {
        await using var fixture = new Fixture(); await fixture.Import();
        var incoming = fixture.Directory.File("incoming.seqvium"); var outgoing = fixture.Directory.File("outgoing.seqvium");
        ProjectPersistence.Save(ProjectDocument.Create(), incoming);
        Assert.True(await fixture.Workflow.OpenAsync(incoming, UnsavedDecision.Save, outgoing));
        Assert.True(fixture.Document.IsClosed);
        var saved = ProjectPersistence.Open(outgoing);
        Assert.Single(saved.Document.Current.State.Graphs); Assert.False(saved.IsDegraded);
        saved.Document.Close();
        Assert.Empty(fixture.Workflow.State.Document.Current.State.Graphs);
    }

    [Fact]
    public async Task FailedSaveDuringCloseRetainsStoppedDocumentAndBorrowerUntilRetry()
    {
        await using var fixture = new Fixture(); await fixture.Import();
        await fixture.Drive(fixture.Workflow.PlayAsync());
        var snapshot = fixture.Document.Current;
        var blocked = fixture.Directory.File("directory-target"); Directory.CreateDirectory(blocked);
        Assert.False(await fixture.Workflow.CloseAsync(UnsavedDecision.Save, blocked));
        Assert.Equal("Workflow.SaveFailed", fixture.Workflow.State.StatusKey);
        Assert.Same(snapshot, fixture.Document.Current); Assert.False(fixture.Document.IsClosed);
        Assert.False(fixture.Output!.Joined); Assert.True(fixture.Sampler!.LivePreparedStates > 0);
        Assert.True(await fixture.Workflow.CloseAsync(UnsavedDecision.Discard));
        Assert.True(fixture.Output.Joined); Assert.Equal(0, fixture.Sampler.LivePreparedStates);
    }

    [Fact]
    public async Task FailedOpenPreservesCurrentDocumentAndPlayback()
    {
        await using var fixture = new Fixture(); await fixture.Import();
        await fixture.Drive(fixture.Workflow.PlayAsync());
        var snapshot = fixture.Document.Current;
        Assert.False(await fixture.Workflow.OpenAsync(fixture.Directory.File("missing.seqvium"), UnsavedDecision.Discard));
        Assert.Same(snapshot, fixture.Document.Current); Assert.False(fixture.Document.IsClosed);
        Assert.True(fixture.Sampler!.ReadStatus().Playing); Assert.False(fixture.Output!.Joined);
        Assert.Equal("Workflow.OpenFailed", fixture.Workflow.State.StatusKey);
    }

    [Fact]
    public async Task FailedSaveRemainsVisibleWhileExistingPlaybackContinues()
    {
        await using var fixture = new Fixture(); await fixture.Import();
        await fixture.Drive(fixture.Workflow.PlayAsync());
        var blocked = fixture.Directory.File("directory-target"); Directory.CreateDirectory(blocked);
        Assert.False(await fixture.Workflow.SaveAsync(blocked));
        Assert.Equal("Workflow.SaveFailed", fixture.Workflow.State.StatusKey);
        Assert.True(fixture.Workflow.State.Execution!.Execution.Playing);
        Assert.True(fixture.Document.IsDirty); Assert.Null(fixture.Document.SavedPath);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InvalidGraphOrMissingMediaReopensHonestlyWithoutOldAudio(bool invalidGraph)
    {
        await using var fixture = new Fixture(); await fixture.Import();
        var graph = fixture.Document.Current.State.Graphs.Single();
        if (invalidGraph)
        {
            // Build invalid intent in a separate document before handing it to the host owner.
            using var stream = new MemoryStream(ProjectPersistence.Encode(fixture.Document));
            var other = ProjectPersistence.Read(stream).Document;
            other.Edit("Invalid Gain", edit => edit.SetGraphGain(graph.Id,
                graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain).Id, 2m));
            var path = fixture.Directory.File("invalid.seqvium"); ProjectPersistence.Save(other, path); other.Close();
            Assert.True(await fixture.Workflow.OpenAsync(path, UnsavedDecision.Discard));
            Assert.Equal("Workflow.Blocked", fixture.Workflow.State.StatusKey);
            Assert.False(await fixture.Workflow.PlayAsync());
            Assert.True(await fixture.Workflow.SaveAsync(fixture.Directory.File("retained.seqvium")));
        }
        else
        {
            var path = fixture.Directory.File("missing.seqvium"); Assert.True(await fixture.Workflow.SaveAsync(path));
            File.Delete(Directory.GetFiles(path + ".media", "*.wav", SearchOption.AllDirectories).Single());
            Assert.True(await fixture.Workflow.OpenAsync(path, UnsavedDecision.Discard));
            Assert.Equal("Workflow.Degraded", fixture.Workflow.State.StatusKey);
            Assert.False(fixture.Workflow.State.CanPlay);
        }
        Assert.Null(fixture.Workflow.State.Execution); Assert.Equal(0, fixture.Opens);
    }

    [Fact]
    public async Task StopAndPanicStayStickyAfterCanonicalGainEdit()
    {
        await using var fixture = new Fixture(); await fixture.Import();
        await fixture.Drive(fixture.Workflow.PlayAsync());
        foreach (bool panic in new[] { false, true })
        {
            await fixture.Workflow.StopAsync(panic); fixture.Sampler!.Process(new float[64]);
            Assert.True(await fixture.Workflow.SetGainAsync(panic ? 0.6m : 0.5m));
            await fixture.Workflow.ObserveAsync();
            var pcm = new float[64]; fixture.Sampler.Process(pcm);
            Assert.All(pcm, value => Assert.Equal(0f, value)); Assert.False(fixture.Sampler.ReadStatus().Playing);
        }
    }

    [Fact]
    public async Task ShutdownDuringPendingPreparationCancelsStartJoinsWorkerThenBorrower()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool exited = false;
        async Task Gate(CancellationToken cancellation)
        {
            entered.TrySetResult();
            try { await Task.Delay(Timeout.Infinite, cancellation); }
            finally { exited = true; }
        }
        await using var fixture = new Fixture(gate: Gate); await fixture.Import();
        var play = fixture.Workflow.PlayAsync();
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        fixture.Output!.BeforeJoin = () => Assert.True(exited);
        Assert.True(await fixture.Workflow.CloseAsync(UnsavedDecision.Discard));
        Assert.False(await play); Assert.True(fixture.Output.Joined); Assert.True(exited);
        Assert.Equal(0, fixture.Sampler!.LivePreparedStates);
        Assert.Equal(0, fixture.Sampler.ReservedPcmAndScratchBytes); Assert.True(fixture.Document.IsClosed);
    }

    [Fact]
    public async Task FailedJoinKeepsDocumentAndPcmUntilExplicitRetry()
    {
        await using var fixture = new Fixture(); await fixture.Import();
        await fixture.Drive(fixture.Workflow.PlayAsync());
        fixture.Output!.FailJoin = true;
        Assert.False(await fixture.Workflow.CloseAsync(UnsavedDecision.Discard));
        Assert.False(fixture.Document.IsClosed); Assert.True(fixture.Sampler!.LivePreparedStates > 0);
        fixture.Output.FailJoin = false;
        Assert.True(await fixture.Workflow.CloseAsync(UnsavedDecision.Discard));
        Assert.Equal(0, fixture.Sampler.LivePreparedStates);
    }

    [Fact]
    public async Task OutputFaultIsVisibleAndNeverAutomaticallyReopens()
    {
        await using var fixture = new Fixture(); await fixture.Import();
        await fixture.Drive(fixture.Workflow.PlayAsync()); fixture.Fault = "Injected device loss";
        // The real host poll observes the fault. State notification is the completion barrier.
        var observed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Workflow.Changed += state =>
        { if (state.StatusKey == "Workflow.DeviceFault" && state.Device is null) observed.TrySetResult(); };
        await observed.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, fixture.Opens); Assert.True(fixture.Output!.Joined);
        Assert.Equal(0, fixture.Sampler!.LivePreparedStates); Assert.False(fixture.Document.IsClosed);
    }

    [Fact]
    public void AllWorkflowLabelsHaveBothLanguages()
    {
        var keys = HostLocalizer.English.Keys.Where(key => key.StartsWith("Workflow.", StringComparison.Ordinal)).ToArray();
        Assert.True(keys.Length >= 30);
        Assert.All(keys, key => Assert.True(HostLocalizer.Russian.ContainsKey(key), key));
    }

    [Fact]
    public async Task ExistingBranchingGraphAndInvalidNumericGainRemainInspectableAndRepairable()
    {
        var graph = new GraphFixture();
        graph.Document.Edit("Invalid preserved Gain", edit => edit.SetGraphGain(graph.Attachment.GraphId, graph.Gain.Id, 2m));
        await using var fixture = new Fixture(graph.Document);
        var state = await fixture.Workflow.ObserveAsync();
        Assert.Contains("Mix", state.Chain); Assert.DoesNotContain("→", state.Chain);
        Assert.Equal(2m, state.Gain); Assert.Equal("Workflow.Blocked", state.StatusKey);
        Assert.False(state.CanPlay); Assert.Contains(GraphReasons.InvalidGain, state.Detail!);
        Assert.True(await fixture.Workflow.SetGainAsync(0.5m));
        Assert.Equal(0.5m, fixture.Workflow.State.Gain);
        Assert.True(Assert.Single(GraphDiagnostics.Inspect(graph.Document.Current.State)).IsEligibleForPreparation);
    }

    [Fact]
    public async Task ReplacingDocumentJoinsPendingPreparationAndDoesNotCreateNewAudio()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool exited = false;
        async Task Gate(CancellationToken cancellation)
        {
            entered.TrySetResult();
            try { await Task.Delay(Timeout.Infinite, cancellation); }
            finally { exited = true; }
        }
        await using var fixture = new Fixture(gate: Gate); await fixture.Import();
        var incoming = fixture.Directory.File("empty.seqvium"); ProjectPersistence.Save(ProjectDocument.Create(), incoming);
        var play = fixture.Workflow.PlayAsync();
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        await fixture.Workflow.StopAsync();
        fixture.Output!.BeforeJoin = () => Assert.True(exited);
        Assert.True(await fixture.Workflow.OpenAsync(incoming, UnsavedDecision.Discard));
        Assert.False(await play); Assert.True(exited); Assert.True(fixture.Output.Joined);
        Assert.True(fixture.Document.IsClosed); Assert.Null(fixture.Workflow.State.Execution);
        Assert.Equal(1, fixture.Opens); Assert.Equal(0, fixture.Sampler!.ReservedPcmAndScratchBytes);
    }

    [Fact]
    public async Task ImportDuringPlaybackSelectsNewUseAndStopsUntilExplicitPlay()
    {
        await using var fixture = new Fixture(); await fixture.Import();
        await fixture.Drive(fixture.Workflow.PlayAsync());
        var first = fixture.Workflow.State.Selected;
        await fixture.Import();
        Assert.NotEqual(first, fixture.Workflow.State.Selected);
        for (int pass = 0; pass < 200; pass++)
        {
            await fixture.Workflow.ObserveAsync();
            var pcm = new float[64]; fixture.Sampler!.Process(pcm);
            Assert.All(pcm, value => Assert.Equal(0f, value));
            Assert.False(fixture.Sampler.ReadStatus().Playing);
            if (fixture.Sampler.ReadStatus().AttachmentId == fixture.Workflow.State.Selected) break;
        }
        Assert.Equal(fixture.Workflow.State.Selected, fixture.Sampler!.ReadStatus().AttachmentId);
    }
}
